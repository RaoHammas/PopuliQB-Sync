using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbRefundServiceQuick
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopuliAccessService _populiAccessService;
    private readonly PopRefundToQbChequeBuilder _builder;
    private readonly QbCustomerService _customerService;
    private readonly QbDepositServiceQuick _depositServiceQuick;
    private readonly QbItemService _itemsService;
    private readonly CustomFieldBuilderQuick _customFieldBuilderQuick;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbCheque> AllExistingChequesList { get; set; } = new();


    public QbRefundServiceQuick(
        PopRefundToQbChequeBuilder builder,
        PopuliAccessService populiAccessService,
        QbCustomerService customerService,
        QbDepositServiceQuick depositServiceQuick,
        QbItemService itemsService,
        CustomFieldBuilderQuick customFieldBuilderQuick
    )
    {
        _builder = builder;
        _populiAccessService = populiAccessService;

        _customerService = customerService;
        _depositServiceQuick = depositServiceQuick;
        _itemsService = itemsService;
        _customFieldBuilderQuick = customFieldBuilderQuick;
    }


    public bool AddRefund(PopPerson person, PopTransaction trans, PopRefund refund,
        QBSessionManager sessionManager)
    {
        try
        {
            var key = $"R:{refund.RefundId}##";

            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var qbStudent = _customerService.AllExistingCustomersList
                .FirstOrDefault(x =>
                    x.QbCustomerFName == person.FirstName!.Trim() 
                    && x.QbCustomerLName == person.LastName!.Trim());
            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"Student: {person.DisplayName!} | Id: {person.Id!} not found in QB."));

                return false;
            }

            /*var existing =
                AllExistingChequesList.FirstOrDefault(
                    x => x.UniqueId == key && x.QbCustomerListId == qbStudent.QbListId);
            if (existing != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped Refund: Refund.Num: {refund.RefundId} already exists as Cheque for: {person.DisplayName!}"));

                return false;
            }*/

            var refundCheque = new PopCredit
            {
                Id = refund.Id,
                Number = refund.RefundId,
                TransactionId = refund.TransactionId,
                PostedOn = refund.PostedDate,
                Object = "Refund",
                ActorType = "person",
                ActorId = person.Id!,
                Amount = refund.Amount,
                Items = new List<PopItem>
                {
                    new()
                    {
                        Name = refund.ReportData.AidName,
                        ItemType = $"{refund.ReportData.AidType}",
                        Amount = refund.Amount,
                        Description = $"{refund.ReportData.AidType} | {refund.Type}",
                        Object = "invoice_item",
                    }
                },
            };


            if (refundCheque.Items != null)
            {
                if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                {
                    var sbItems = refundCheque.Items
                        .Where(x => x.Name == QbSettings.Instance.SkipStartingBalanceItemName).ToList();
                    foreach (var sbItem in sbItems)
                    {
                        refundCheque.Items.Remove(sbItem);
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Skipped: refund.Num = {refund.RefundId} | Item {sbItem.Name} for student: {person.DisplayName!}."));
                    }
                }

                if (!refundCheque.Items.Any())
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Warn,
                            $"Skipped: refund.Num = {refund.RefundId}. It has no items for student: {person.DisplayName!}."));
                    return false;
                }

                foreach (var item in refundCheque.Items)
                {
                    if (!_itemsService.CheckIfItemExists(item))
                    { 
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"refund.Num = {refund.RefundId} | Item {item.Name} doesn't exist in QB."));
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Skipped rest of the Refunds for student {person.DisplayName} due to Item not found issue."));
                        return false;
                        
                    }
                }
            }

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId)
                .ToList();
            var convEntries = trans.LedgerEntries.Where(x => x.AccountId == QbSettings.Instance.PopConvenienceAccId)
                .ToList();

            var bankAccId = nonConvEntries.First(x => x.Direction == "credit").AccountId!;
            var recAccId = nonConvEntries.First(x => x.Direction == "debit").AccountId!;
            var bankQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == bankAccId).QbAccountListId;
            var recQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == recAccId).QbAccountListId;

            _builder.BuildAddRequest(requestMsgSet, key, refundCheque, qbStudent.QbListId!, bankQbAccListId!,
                recQbAccListId!, refund.PostedDate!.Value);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedCheque(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));

                return false;
            }

            
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Refund num: {refund.RefundId} Added as Cheque num: {refund.RefundId} for student: {person.DisplayName}."));

            if (convEntries.Any())
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Adding convenience fee as Deposit for Payment.Num: {refund.RefundId} for student: {person.DisplayName}"));

                foreach (var convEntry in convEntries)
                {
                    var payment = new PopPayment
                    {
                        Id = refund.Id,
                        Number = $"Conv:{convEntry.Id}-{key}",
                        StudentId = person.Id,
                        ConvenienceFeeAmount = convEntry.Credit!.Value,
                        TransactionId = convEntry.TransactionId,
                    };

                    var arAcc = convEntry.AccountId!.Value;
                    var adAcc = trans.LedgerEntries.First(x => x.Direction == "debit" && x.Debit!.Value! == convEntry.Credit!.Value).AccountId!.Value;
                    
                    var resp = _depositServiceQuick.AddDeposit(payment, payment.Number.ToString()!, qbStudent, arAcc, adAcc, trans.PostedOn, sessionManager);
                    if (resp)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Success,
                                $"Added convenience fee as Deposit for Refund.Num: {refund.RefundId} for student: {person.DisplayName}"));
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"Failed to add convenience fee as Deposit for Refund.Num: {refund.RefundId} for student: {person.DisplayName}. Add manually!"));
                    }
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


    #region CHEQUES

    public async Task SyncAllExistingChequesAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            AllExistingChequesList.Clear();

            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _builder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedCheques(responseMsgSet))
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
                    $"Completed: Found Cheques in QB {AllExistingChequesList.Count}"));
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

    private bool ReadFetchedCheques(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCheckQueryRs)
                    {
                        var retList = (ICheckRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesCheque(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedCheque(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCheckAddRs)
                    {
                        var ret = (ICheckRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesCheque(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbCheque? ReadPropertiesCheque(ICheckRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var cheque = new QbCheque();
            cheque.QbCustomerListId = ret.PayeeEntityRef.ListID.GetValue();
            cheque.QbCustomerName = ret.PayeeEntityRef.FullName.GetValue();
            var refNum = ret.Memo.GetValue();
            cheque.UniqueId = "";
            if (!string.IsNullOrEmpty(refNum))
            {
                var arr = refNum.Split("##");
                if (arr.Any())
                {
                    cheque.UniqueId = arr[0].Trim() + "##";
                }
            }
            

            AllExistingChequesList.Add(cheque);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Cheque: {cheque.UniqueId}"));
            return cheque;
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