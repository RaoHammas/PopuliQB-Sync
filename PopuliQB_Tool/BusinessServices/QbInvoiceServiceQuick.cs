using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbInvoiceServiceQuick
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopInvoiceToQbInvoiceBuilder _invoiceBuilder;
    private readonly QbCustomerService _customerService;
    private readonly QbItemService _itemService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QbCreditMemoServiceQuick _creditMemoServiceQuick;
    private readonly QbDepositServiceQuick _depositServiceQuick;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbInvoice> AllExistingInvoicesList { get; set; } = new();

    public QbInvoiceServiceQuick(
        PopInvoiceToQbInvoiceBuilder invoiceBuilder,
        QbCustomerService customerService,
        QbItemService itemService,
        PopuliAccessService populiAccessService,
        QbCreditMemoServiceQuick creditMemoServiceQuick,
        QbDepositServiceQuick depositServiceQuick
    )
    {
        _invoiceBuilder = invoiceBuilder;
        _customerService = customerService;
        _itemService = itemService;
        _populiAccessService = populiAccessService;
        _creditMemoServiceQuick = creditMemoServiceQuick;
        _depositServiceQuick = depositServiceQuick;
    }

    public async Task<bool> AddInvoiceAsync(PopPerson person, PopTransaction trans, PopInvoice invoice,
        QBSessionManager sessionManager)
    {
        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var qbStudent =
                _customerService.AllExistingCustomersList.FirstOrDefault(x =>
                    x.PopPersonId == person.Id!.Value);

            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"{person.DisplayName!} | Id = {person.Id!.Value} student not found."));

                return false;
            }

            var existingInv =
                AllExistingInvoicesList.FirstOrDefault(x =>
                    x.PopInvoiceNumber == invoice.Number &&
                    x.QbCustomerListId == qbStudent.QbListId);
            if (existingInv != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped: Invoice.Num: {invoice.Number} already exists for student: {person.DisplayName}"));
                return false;
            }

            if (invoice.Items != null)
            {
                foreach (var invoiceItem in invoice.Items)
                {
                    if (invoiceItem.Name!.Length > 31) //31 is max length for Item name field in QB
                    {
                        var name = invoiceItem.Name.Substring(0, 31).Trim();
                        invoiceItem.Name = name.RemoveInvalidUnicodeCharacters();
                    }

                    var existingItem = _itemService.AllExistingItemsList.FirstOrDefault(x =>
                        x.QbItemName!.ToLower().Trim() == invoiceItem.Name.ToLower().Trim());
                    if (existingItem == null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"Invoice.Num = {invoice.Number} | Invoice Item {invoiceItem.Name} doesn't exist in QB."));

                        return false;
                    }

                    invoiceItem.ItemQbListId = existingItem!.QbListId;
                }
            }

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId)
                .ToList();
            var convEntries = trans.LedgerEntries.Where(x => x.AccountId == QbSettings.Instance.PopConvenienceAccId)
                .ToList();

            var arAccId = nonConvEntries.First(x => x.Direction == "debit").AccountId!;
            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;

            _invoiceBuilder.BuildInvoiceAddRequest(requestMsgSet, invoice, qbStudent.QbListId!,
                arQbAccListId!);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedInvoice(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"{person.DisplayName!} | Invoice.Num = {invoice.Number} | {msg}"));

                return false;
            }

           
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Invoice num: {invoice.Number} added for student: {person.DisplayName}."));

            if (invoice.Credits != null && invoice.Credits.Any())
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Sales Credit found for student: {person.DisplayName!}"));

                foreach (var invoiceCredit in invoice.Credits)
                {
                    var resp = await _creditMemoServiceQuick.AddCreditMemoForSalesCredit(person, invoiceCredit,
                        sessionManager);
                }
            }

            if (convEntries.Any())
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Adding convenience fee as Deposit for Invoice.Num: {invoice.Number} for student: {person.DisplayName}"));

                var payment = new PopPayment
                {
                    Id = invoice.Id,
                    Number = invoice.Number,
                    StudentId = person.Id,
                    ConvenienceFeeAmount = convEntries.First(x => x.Debit > 0).Debit,
                };

                var resp = _depositServiceQuick.AddDeposit(trans, payment, qbStudent, sessionManager);
                if (resp)
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Success,
                            $"Added convenience fee as Deposit for Invoice.Num: {invoice.Number} for student: {person.DisplayName}"));
                }
                else
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Error,
                            $"Failed to add convenience fee as Deposit for Invoice.Num: {invoice.Number} for student: {person.DisplayName}. Add manually!"));
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

    public bool AddInvoiceForRefundToSourceAsync(PopPerson person, PopTransaction trans, PopRefund refund,
        QBSessionManager sessionManager)
    {
        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var qbStudent =
                _customerService.AllExistingCustomersList.FirstOrDefault(x =>
                    x.PopPersonId == person.Id!.Value);
            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"{person.DisplayName!} | Id = {person.Id!.Value} student not found."));

                return false;
            }

            var existingInv =
                AllExistingInvoicesList.FirstOrDefault(x =>
                    x.PopInvoiceNumber == refund.RefundId &&
                    x.QbCustomerListId == qbStudent.QbListId);
            if (existingInv != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped: RefundToSource: {refund.RefundId} already exists as Invoice.Num: {refund.RefundId} for student: {person.DisplayName}"));
                return false;
            }

            var invoice = new PopInvoice
            {
                Id = refund.Id,
                Number = refund.RefundId,
                Amount = refund.Amount,
                Description = $"Refund to Source: {refund.RefundId} as Invoice.",
                PostedOn = refund.PostedDate!.Value,
                TransactionId = refund.TransactionId,
                ActorId = person.Id,
                ActorType = "Person",
                Items = new List<PopItem>
                {
                    new()
                    {
                        Amount = refund.Amount,
                        Description = $"{refund.ReportData.AidType} | {refund.Type}",
                        Name = refund.ReportData.AidName,
                        ItemType = $"{refund.ReportData.AidType}",
                    }
                }
            };

            if (invoice.Items != null)
            {
                foreach (var invoiceItem in invoice.Items)
                {
                    if (invoiceItem.Name!.Length > 31) //31 is max length for Item name field in QB
                    {
                        var name = invoiceItem.Name.Substring(0, 31).Trim();
                        invoiceItem.Name = name.RemoveInvalidUnicodeCharacters();
                    }

                    var existingItem = _itemService.AllExistingItemsList.FirstOrDefault(x =>
                        x.QbItemName!.ToLower().Trim() == invoiceItem.Name.ToLower().Trim());
                    if (existingItem == null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"refund.Num = {refund.RefundId} | Item {invoiceItem.Name} doesn't exist in QB."));

                        return false;
                    }

                    invoiceItem.ItemQbListId = existingItem!.QbListId;
                }
            }

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId)
                .ToList();
            var convEntries = trans.LedgerEntries.Where(x => x.AccountId == QbSettings.Instance.PopConvenienceAccId)
                .ToList();

            var arAccId = nonConvEntries.First(x => x.Direction == "debit").AccountId!;
            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;

            _invoiceBuilder.BuildInvoiceAddRequest(requestMsgSet, invoice, qbStudent.QbListId!, arQbAccListId!);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedInvoice(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"{person.DisplayName!} | Invoice.Num = {invoice.Number} | {msg}"));

                return false;
            }

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"RefundToSource: {refund.RefundId} added as Invoice.Num: {invoice.Number} for student: {person.DisplayName!}"));

            if (convEntries.Any())
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Adding convenience fee as Deposit for Payment.Num: {refund.RefundId} for student: {person.DisplayName}"));

                var payment = new PopPayment
                {
                    Id = refund.Id,
                    Number = refund.RefundId,
                    StudentId = person.Id,
                    ConvenienceFeeAmount = convEntries.First(x => x.Debit > 0).Debit,
                };

                var resp = _depositServiceQuick.AddDeposit(trans, payment, qbStudent, sessionManager);
                if (resp)
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Success,
                            $"Added convenience fee as Deposit for RefundToSource.Num: {refund.RefundId} for student: {person.DisplayName}"));
                }
                else
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Error,
                            $"Failed to add convenience fee as Deposit for RefundToSource.Num: {refund.RefundId} for student: {person.DisplayName}. Add manually!"));
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

    #region INVOICES

    public async Task SyncAllExistingInvoicesAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            AllExistingInvoicesList.Clear();

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

                _invoiceBuilder.BuildGetAllInvoicesRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                if (!ReadFetchedInvoices(responseMsgSet))
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
                    $"Completed: Found Invoices in QB {AllExistingInvoicesList.Count}"));
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

    private bool ReadFetchedInvoices(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtInvoiceQueryRs)
                    {
                        var retList = (IInvoiceRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));
                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesInvoice(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedInvoice(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtInvoiceAddRs)
                    {
                        var ret = (IInvoiceRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesInvoice(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbInvoice? ReadPropertiesInvoice(IInvoiceRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var invoice = new QbInvoice();
            invoice.PopInvoiceId = Convert.ToInt32(ret.PONumber.GetValue()); //unique inv id from populi
            invoice.PopInvoiceNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            invoice.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            invoice.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingInvoicesList.Add(invoice);

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Invoice: {invoice.PopInvoiceNumber}"));
            return invoice;
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