using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QBInvoiceService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopInvoiceToQbInvoiceBuilder _invoiceBuilder;
    private readonly PopCreditMemoToQbCreditMemoBuilder _memoBuilder;
    private readonly PopPaymentToQbPaymentBuilder _paymentBuilder;
    private readonly QbCustomerService _customerService;
    private readonly QbItemService _itemService;
    private readonly PopuliAccessService _populiAccessService;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbInvoice> AllExistingInvoicesList { get; set; } = new();
    public List<QbMemo> AllExistingMemosList { get; set; } = new();
    public List<QbPayment> AllExistingPaymentsList { get; set; } = new();

    public QBInvoiceService(
        PopInvoiceToQbInvoiceBuilder invoiceBuilder,
        PopCreditMemoToQbCreditMemoBuilder memoBuilder,
        PopPaymentToQbPaymentBuilder paymentBuilder,
        QbCustomerService customerService,
        QbItemService itemService,
        PopuliAccessService populiAccessService
    )
    {
        _invoiceBuilder = invoiceBuilder;
        _memoBuilder = memoBuilder;
        _paymentBuilder = paymentBuilder;
        _customerService = customerService;
        _itemService = itemService;
        _populiAccessService = populiAccessService;
    }

    public async Task AddInvoicesAsync(List<PopInvoice> invoices)
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(async () =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                foreach (var invoice in invoices)
                {
                    var personFullName = "";
                    if (invoice.ReportData != null)
                    {
                        personFullName = PopPersonToQbCustomerBuilder.GetFullName(invoice.ReportData.Firstname ?? "",
                            invoice.ReportData.Lastname ?? "");
                    }

                    var existingCustomer =
                        _customerService.AllExistingCustomersList.FirstOrDefault(x =>
                            x.PopPersonId == invoice.ReportData?.PersonId);

                    if (existingCustomer != null)
                    {
                        IMsgSetResponse responseMsgSet;
                        var existingInv =
                            AllExistingInvoicesList.FirstOrDefault(x => x.PopInvoiceNumber == invoice.Number);
                        if (existingInv != null)
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Warn,
                                    $"Skipped: Invoice.Num: {invoice.Number} already exists for customer: {existingInv.QbCustomerName}"));
                        }
                        else
                        {
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
                                                $"{personFullName} | Invoice.Num = {invoice.Number} | Invoice Item {invoiceItem.Name} doesn't exist in QB."));

                                        return;
                                    }

                                    invoiceItem.ItemQbListId = existingItem!.QbListId;
                                }
                            }

                            var trans = await _populiAccessService.GetTransactionWithLedgerAsync(
                                invoice.TransactionId!.Value);

                            if (trans == null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"Transaction not found for Invoice Number# {invoice.Number}"));
                                continue;
                            }

                            var arAccId = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;
                            var arQbAccListId = _populiAccessService.AllPopuliAccounts
                                .First(x => x.Id == arAccId).QbAccountListId;

                            _invoiceBuilder.BuildInvoiceAddRequest(requestMsgSet, invoice, existingCustomer.QbListId!,
                                arQbAccListId!);
                            responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                            if (!ReadAddedInvoice(responseMsgSet))
                            {
                                var xmResp = responseMsgSet.ToXMLString();
                                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"{personFullName} | Invoice.Num = {invoice.Number} | {msg}"));
                            }
                            else
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Success,
                                        $"{personFullName} | Invoice.Num = {invoice.Number}"));
                            }
                        }


                        //SetUp Student AID Awards
                        var studentAwards =
                            await _populiAccessService.FetchAllAStudentAwardsAsync(invoice.ReportData!.PersonId!.Value,
                                invoice.ReportData!.DisplayName!);
                        var allCredits = new List<PopCredit>();

                        //Add Payments-----------------
                        if (invoice.Payments != null && invoice.Payments.Any())
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"{personFullName} | Payments Found: {invoice.Payments.Count}"));

                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"{personFullName} | Adding Payments."));

                            foreach (var payment in invoice.Payments)
                            {
                                if (QbSettings.Instance.ApplyAidPaymentsAreCreditMemoFilter &&
                                    payment is { PaidByType: "aid_provider", AidTypeId: not null })
                                {
                                    var transAid = await _populiAccessService.GetTransactionWithLedgerAsync(
                                        payment.TransactionId!.Value);

                                    if (transAid == null)
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error,
                                                $"Transaction not found for Payment Number# {payment.Number}"));
                                        continue;
                                    }

                                    var aid = studentAwards.First(
                                        x => x.AidTypeId == payment.AidTypeId.Value);
                                    var item = new PopCredit
                                    {
                                        Id = payment.Id,
                                        Number = payment.Number,
                                        TransactionId = payment.TransactionId,
                                        PostedOn = transAid.PostedOn,
                                        Object = "credit",
                                        ActorType = "person",
                                        ActorId = invoice.ReportData.PersonId!,
                                        Amount = payment.Amount,
                                        Items = new List<PopItem>(),
                                    };

                                    item.Items.Add(new PopItem
                                    {
                                        Name = aid.ReportData!.AidName,
                                        Id = aid.Id,
                                        ItemType = "aid_provider",
                                        Amount = payment.Amount,
                                        Description = "",
                                        InvoiceId = invoice.Id,
                                        ItemId = aid.AidTypeId,
                                        Object = "invoice_item",
                                    });

                                    allCredits.Add(item);
                                    continue;
                                }


                                var existingPay =
                                    AllExistingPaymentsList.FirstOrDefault(
                                        x => x.PopPaymentNumber == payment.Number);
                                if (existingPay != null)
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Warn,
                                            $"Skipped Payment: Payment.Num: {payment.Number} already exists for: {existingPay.QbCustomerName}"));

                                    continue;
                                }

                                var trans = await _populiAccessService.GetTransactionWithLedgerAsync(
                                    payment.TransactionId!.Value);

                                if (trans == null)
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Error,
                                            $"Transaction not found for Payment Number# {payment.Number}"));
                                    continue;
                                }

                                var arAccId = trans.LedgerEntries.First(x => x.Direction == "credit")
                                    .AccountId!;
                                //var adAccId = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

                                var arQbAccListId = _populiAccessService.AllPopuliAccounts
                                    .First(x => x.Id == arAccId).QbAccountListId;
                                /*var adQbAccListId = _populiAccessService.AllPopuliAccounts
                                    .First(x => x.Id == adAccId).QbAccountListId;*/

                                _paymentBuilder.BuildAddRequest(requestMsgSet, payment,
                                    existingCustomer.QbListId!, arQbAccListId!,
                                    QbSettings.Instance.ADForPayments.ListId);

                                responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                                if (!ReadAddedPayments(responseMsgSet))
                                {
                                    var xmResp = responseMsgSet.ToXMLString();
                                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                                }
                                else
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Success,
                                            $"{personFullName} | Payment.Num = {payment.Number}"));
                                }
                            }
                        }


                        if (invoice.Credits != null && invoice.Credits.Any())
                        {
                            allCredits.AddRange(invoice.Credits);
                        }
                        //Add Credit Memos-------------
                        if (allCredits.Any())
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"{personFullName} | Credit Memos Found: {allCredits.Count}"));
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"{personFullName} | Adding Memos."));

                            foreach (var memo in allCredits.ToList())
                            {
                                var existingMemo =
                                    AllExistingMemosList.FirstOrDefault(x => x.PopMemoNumber == memo.Number);
                                if (existingMemo != null)
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Warn,
                                            $"Skipped Memo: Memo.Num: {existingMemo.PopMemoNumber} already exists for: {existingMemo.QbCustomerName}"));

                                    continue;
                                }

                                if (memo.Items != null)
                                {
                                    if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                                    {
                                        var sbItems = memo.Items.Where(x => x.Name == "Starting Balance").ToList();
                                        foreach (var sbItem in sbItems)
                                        {
                                            memo.Items.Remove(sbItem);

                                            OnSyncStatusChanged?.Invoke(this,
                                                new StatusMessageArgs(StatusMessageType.Warn,
                                                    $"{personFullName} | Memo.Num = {invoice.Number} | Memo Item {sbItem.Name} skipped."));
                                        }
                                    }

                                    if (!memo.Items.Any())
                                    {
                                        
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Warn,
                                                $"{personFullName} | Memo.Num = {invoice.Number} skipped. It has no items."));
                                        continue;
                                    }

                                    foreach (var invoiceCredItem in memo.Items)
                                    {
                                        if (invoiceCredItem.Name!.Length >
                                            31) //31 is max length for Item name field in QB
                                        {
                                            var name = invoiceCredItem.Name.Substring(0, 31).Trim();
                                            invoiceCredItem.Name = name.RemoveInvalidUnicodeCharacters();
                                        }

                                        var existingItem = _itemService.AllExistingItemsList.FirstOrDefault(x =>
                                            x.QbItemName!.ToLower().Trim() ==
                                            invoiceCredItem.Name.ToLower().Trim());
                                        if (existingItem == null)
                                        {
                                            OnSyncStatusChanged?.Invoke(this,
                                                new StatusMessageArgs(StatusMessageType.Error,
                                                    $"{personFullName} | CreditMemo.Num = {memo.Number} | Memo Item {invoiceCredItem.Name} doesn't exist in QB."));

                                            continue;
                                        }

                                        invoiceCredItem.ItemQbListId = existingItem!.QbListId;
                                    }
                                }

                                var trans = await _populiAccessService.GetTransactionWithLedgerAsync(
                                    memo.TransactionId!.Value);

                                if (trans == null)
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Error,
                                            $"Transaction not found for Credit Invoice Number# {memo.Number}"));
                                    continue;
                                }

                                var arAccId = trans.LedgerEntries.First(x => x.Direction == "credit")
                                    .AccountId!;
                                var arQbAccListId = _populiAccessService.AllPopuliAccounts
                                    .First(x => x.Id == arAccId).QbAccountListId;

                                _memoBuilder.BuildAddRequest(requestMsgSet, memo,
                                    existingCustomer.QbListId!, arQbAccListId!);

                                responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                                if (!ReadAddedMemo(responseMsgSet))
                                {
                                    var xmResp = responseMsgSet.ToXMLString();
                                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                                }
                                else
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Success,
                                            $"{personFullName} | Memo.Num = {memo.Number}"));
                                }
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"{personFullName} | Id = {invoice.ReportData?.PersonId} Customer not found."));
                    }

                    OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));
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

                _memoBuilder.BuildGetAllRequest(requestMsgSet);
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


    #region PAYMENTS

    public async Task SyncAllExistingPaymentsAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            AllExistingPaymentsList.Clear();

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

                _paymentBuilder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedPayments(responseMsgSet))
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
                    $"Completed: Found Payments in QB {AllExistingPaymentsList.Count}"));
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

    private bool ReadFetchedPayments(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtReceivePaymentQueryRs)
                    {
                        var retList = (IReceivePaymentRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesPayments(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedPayments(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtReceivePaymentAddRs)
                    {
                        var ret = (IReceivePaymentRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesPayments(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbPayment? ReadPropertiesPayments(IReceivePaymentRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var payment = new QbPayment();
            payment.PopPaymentNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            payment.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            payment.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingPaymentsList.Add(payment);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Payment: {payment.PopPaymentNumber}"));
            return payment;
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