using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbCreditMemoServiceQuick
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopuliAccessService _populiAccessService;
    private readonly PopCreditMemoToQbCreditMemoBuilder _builder;
    private readonly QbCustomerService _customerService;
    private readonly QbDepositServiceQuick _depositServiceQuick;
    private readonly QbItemService _itemsService;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbMemo> AllExistingMemosList { get; set; } = new();


    public QbCreditMemoServiceQuick(
        PopCreditMemoToQbCreditMemoBuilder builder,
        PopuliAccessService populiAccessService,
        QbCustomerService customerService,
        QbDepositServiceQuick depositServiceQuick,
        QbItemService itemsService
    )
    {
        _builder = builder;
        _populiAccessService = populiAccessService;

        _customerService = customerService;
        _depositServiceQuick = depositServiceQuick;
        _itemsService = itemsService;
    }


    public async Task<bool> AddCreditMemo(PopPerson person, PopTransaction trans, PopPayment payment,
        QBSessionManager sessionManager)
    {
        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var qbStudent =
                _customerService.AllExistingCustomersList.FirstOrDefault(x => x.UniquePopuliId == person.Id!);
            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"Student: {person.DisplayName!} | Id: {person.Id!} not found in QB."));

                return false;
            }

            var existing =
                AllExistingMemosList.FirstOrDefault(
                    x => x.PopMemoNumber == payment.Number
                         && x.QbCustomerListId == qbStudent.QbListId);
            if (existing != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped Payment: Payment.Num: {payment.Number} already exists as CredMemo for: {person.DisplayName!}"));

                return false;
            }

            //SetUp Student AID Awards
            var aidAwardsList =
                await _populiAccessService.GetAllStudentAwardsAsync(person.Id!.Value, person.DisplayName!);

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId)
                .ToList();

            var aid = aidAwardsList.First(x => x.AidTypeId == payment.AidTypeId!.Value);
            var memo = new PopCredit
            {
                Id = payment.Id,
                Number = payment.Number,
                TransactionId = payment.TransactionId,
                PostedOn = trans.PostedOn,
                Object = "aid_payment",
                ActorType = "person",
                ActorId = person.Id!,
                Amount = payment.Amount,
                Items = new List<PopItem>
                {
                    new()
                    {
                        Name = aid.ReportData!.AidName,
                        Id = aid.Id,
                        ItemType = "aid_provider",
                        Amount = payment.Amount,
                        Description = "",
                        ItemId = aid.AidTypeId,
                        Object = "AidPayment_Item",
                    }
                },
            };

            if (memo.Items != null)
            {
                if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                {
                    var sbItems = memo.Items.Where(x => x.Name == QbSettings.Instance.SkipStartingBalanceItemName)
                        .ToList();
                    foreach (var sbItem in sbItems)
                    {
                        memo.Items.Remove(sbItem);
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"skipped Payment Item: Payment.Num = {memo.Number} | Payment Item {sbItem.Name}."));
                    }
                }

                if (!memo.Items.Any())
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Warn,
                            $"Skipped Payment: Payment.Num = {memo.Number}. It has no items."));

                    return false;
                }

                foreach (var credItem in memo.Items)
                {
                    //31 is max length for Item name field in QB
                    if (credItem.Name!.Length > 31)
                    {
                        var name = credItem.Name.Substring(0, 31).Trim();
                        credItem.Name = name.RemoveInvalidUnicodeCharacters();
                    }

                    var existingItem = _itemsService.AllExistingItemsList.FirstOrDefault(x =>
                        x.QbItemName!.ToLower().Trim() == credItem.Name.ToLower().Trim());
                    if (existingItem == null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"CreditMemo.Num = {memo.Number} | Memo Item {credItem.Name} doesn't exist in QB."));

                        return false;
                    }

                    credItem.ItemQbListId = existingItem!.QbListId;
                }
            }

            var arAccId = nonConvEntries.First(x => x.Direction == "credit").AccountId!;
            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;

            _builder.BuildAddRequest(requestMsgSet, memo, qbStudent.QbListId!, arQbAccListId!);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedMemo(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));

                return false;
            }

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Payment num: {payment.Number} Added as Credit Memo num: {memo.Number} for student: {person.DisplayName}"));

            if (payment.ConvenienceFeeAmount is > 0)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Adding convenience fee as Deposit for Payment.Num: {payment.Number} for student: {person.DisplayName}"));
                var resp = _depositServiceQuick.AddDeposit(trans, payment, qbStudent, sessionManager);
                if (resp)
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Success,
                            $"Added convenience fee as Deposit for Payment.Num: {payment.Number} for student: {person.DisplayName}"));
                }
                else
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Error,
                            $"Failed to add convenience fee as Deposit for Payment.Num: {payment.Number} for student: {person.DisplayName}. Add manually!"));
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return false;
        }
    }

    public async Task<bool> AddCreditMemoForSalesCredit(PopPerson person, PopCredit salesCredit,
        QBSessionManager sessionManager)
    {
        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(salesCredit.TransactionId!.Value);
            if (trans.Id == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"Skipped SalesCredit: Transaction not found for SalesCredit.Num: {salesCredit.Number} for student: {person.DisplayName!}"));

                return false;
            }

            var qbStudent =
                _customerService.AllExistingCustomersList.FirstOrDefault(x => x.UniquePopuliId == person.Id!);
            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"Student: {person.DisplayName!} | Id: {person.Id!} not found in QB."));

                return false;
            }

            var existing =
                AllExistingMemosList.FirstOrDefault(
                    x => x.PopMemoNumber == salesCredit.Number
                         && x.QbCustomerListId == qbStudent.QbListId);
            if (existing != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped SalesCredit: SalesCredit.Num: {salesCredit.Number} already exists as CredMemo for: {person.DisplayName!}"));

                return false;
            }

            if (salesCredit.Items != null)
            {
                if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                {
                    var sbItems = salesCredit.Items
                        .Where(x => x.Name == QbSettings.Instance.SkipStartingBalanceItemName)
                        .ToList();
                    foreach (var sbItem in sbItems)
                    {
                        salesCredit.Items.Remove(sbItem);
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"skipped SalesCredit Item: SalesCredit.Num = {salesCredit.Number} | Item {sbItem.Name}."));
                    }
                }

                if (!salesCredit.Items.Any())
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Warn,
                            $"Skipped SalesCredit: SalesCredit.Num = {salesCredit.Number}. It has no items."));

                    return false;
                }

                foreach (var credItem in salesCredit.Items)
                {
                    //31 is max length for Item name field in QB
                    if (credItem.Name!.Length > 31)
                    {
                        var name = credItem.Name.Substring(0, 31).Trim();
                        credItem.Name = name.RemoveInvalidUnicodeCharacters();
                    }

                    var existingItem = _itemsService.AllExistingItemsList.FirstOrDefault(x =>
                        x.QbItemName!.ToLower().Trim() == credItem.Name.ToLower().Trim());
                    if (existingItem == null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"SaleCredit.Num = {salesCredit.Number} | Item {credItem.Name} doesn't exist in QB."));

                        return false;
                    }

                    credItem.ItemQbListId = existingItem!.QbListId;
                }
            }

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId)
                .ToList();
            var arAccId = nonConvEntries.First(x => x.Direction == "credit").AccountId!;
            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;

            _builder.BuildAddRequest(requestMsgSet, salesCredit, qbStudent.QbListId!, arQbAccListId!);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedMemo(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));

                return false;
            }

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"SalesCredit num: {salesCredit.Number} Added as Credit Memo num: {salesCredit.Number} for student: {person.DisplayName}"));

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return false;
        }
    }


    #region MEMOS

    public async Task SyncAllExistingMemosAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            AllExistingMemosList.Clear();

            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _builder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedMemos(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                    _logger.Error(msg);

                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                }
            });

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Completed: Found Memos in QB {AllExistingMemosList.Count}"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
        }
        finally
        {
            if (isSessionOpen)
            {
                sessionManager.EndSession();
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (isConnected)
            {
                sessionManager.CloseConnection();
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private bool ReadFetchedMemos(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return false;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return false;

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCreditMemoQueryRs)
                    {
                        var retList = (ICreditMemoRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesMemo(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedMemo(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null)
        {
            return false;
        }

        var responseList = responseMsgSet.ResponseList;
        if (responseList == null)
        {
            return false;
        }

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCreditMemoAddRs)
                    {
                        var ret = (ICreditMemoRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesMemo(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbMemo? ReadPropertiesMemo(ICreditMemoRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var memo = new QbMemo();
            memo.PopInvoiceId = Convert.ToInt32(ret.PONumber.GetValue()); //unique memo id from populi
            memo.PopMemoNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            memo.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            memo.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingMemosList.Add(memo);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Memo: {memo.PopMemoNumber}"));
            return memo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{ex.Message}"));
        }

        return null;
    }

    #endregion
}