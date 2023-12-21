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
    private readonly QBInvoiceItemService _invoiceItemService;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }
    public List<QbInvoice> AllExistingInvoicesList { get; set; } = new();
    public List<QbMemo> AllExistingMemosList { get; set; } = new();
    public List<QbPayment> AllExistingPaymentsList { get; set; } = new();

    public QBInvoiceService(
        PopInvoiceToQbInvoiceBuilder invoiceBuilder,
        PopCreditMemoToQbCreditMemoBuilder memoBuilder,
        PopPaymentToQbPaymentBuilder paymentBuilder,
        QbCustomerService customerService,
        QBInvoiceItemService invoiceItemService)
    {
        _invoiceBuilder = invoiceBuilder;
        _memoBuilder = memoBuilder;
        _paymentBuilder = paymentBuilder;
        _customerService = customerService;
        _invoiceItemService = invoiceItemService;
    }

    public async Task<bool> AddInvoicesAsync(List<PopInvoice> invoices)
    {
        var sessionManager = new QBSessionManager();

        try
        {
            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(async () =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                await _invoiceItemService.GetAllExistingInvoiceServiceItemsAsync(sessionManager, requestMsgSet);

                for (var index = 0; index < invoices.Count; index++)
                {
                    var invoice = invoices[index];
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
                        //Check if inv Items exist in QB itemsList otherwise add them
                        foreach (var invoiceItem in invoice.Items!)
                        {
                            var existedInvItem =
                                _invoiceItemService.AllExistingInvoiceServiceItemsList.FirstOrDefault(x =>
                                    x.QbItemName == invoiceItem.Name);
                            if (existedInvItem == null)
                            {
                                await _invoiceItemService.AddInvoiceServiceItemAsync(invoiceItem, sessionManager,
                                    requestMsgSet);
                                invoiceItem.Name = _invoiceItemService.AllExistingInvoiceServiceItemsList.Last()
                                    .QbItemName;
                            }
                        }

                        _invoiceBuilder.BuildInvoiceAddRequest(requestMsgSet, invoice, existingCustomer.QbListId!);
                        var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                        if (ReadAddedInvoice(responseMsgSet))
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Success,
                                    $"{personFullName} | Invoice.Num = {invoice.Number}"));

                            //Add Credit Memos-------------
                            try
                            {
                                if (invoice.Credits != null && invoice.Credits.Any())
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Info,
                                            $"{personFullName} | Credit Memos Found: {invoice.Credits.Count}"));
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Info,
                                            $"{personFullName} | Adding Memos."));

                                    for (var i = 0; i < invoice.Credits.Count; i++)
                                    {
                                        var invoiceCredit = invoice.Credits[i];

                                        //Check if credit Items exist in QB itemsList otherwise add them
                                        foreach (var creditItem in invoiceCredit.Items!)
                                        {
                                            var existedInvItem =
                                                _invoiceItemService.AllExistingInvoiceServiceItemsList.FirstOrDefault(
                                                    x =>
                                                        x.QbItemName == creditItem.Name);
                                            if (existedInvItem == null)
                                            {
                                                await _invoiceItemService.AddInvoiceServiceItemAsync(creditItem,
                                                    sessionManager, requestMsgSet);
                                                creditItem.Name = _invoiceItemService.AllExistingInvoiceServiceItemsList
                                                    .Last().QbItemName;
                                            }
                                        }

                                        _memoBuilder.BuildAddRequest(requestMsgSet, invoiceCredit,
                                            existingCustomer.QbListId!);
                                        responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                                        var msg = responseMsgSet.ToXMLString();
                                        if (ReadAddedMemo(responseMsgSet))
                                        {
                                            OnSyncStatusChanged?.Invoke(this,
                                                new StatusMessageArgs(StatusMessageType.Success,
                                                    $"{personFullName} | Memo.Num = {invoiceCredit.Number}"));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, ex.Message));
                            }


                            //Add Payments-----------------
                            try
                            {
                                if (invoice.Payments != null && invoice.Payments.Any())
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Info,
                                            $"{personFullName} | Payments Found: {invoice.Payments.Count}"));

                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Info,
                                            $"{personFullName} | Adding Payments."));

                                    for (var i = 0; i < invoice.Payments.Count; i++)
                                    {
                                        var invoicePayment = invoice.Payments[i];

                                        _paymentBuilder.BuildAddRequest(requestMsgSet, invoicePayment,
                                            existingCustomer.QbListId!);

                                        responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                                        var msg = responseMsgSet.ToXMLString();
                                        if (ReadAddedPayments(responseMsgSet))
                                        {
                                            OnSyncStatusChanged?.Invoke(this,
                                                new StatusMessageArgs(StatusMessageType.Success,
                                                    $"{personFullName} | Payment.Num = {invoicePayment.Number}"));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, ex.Message));
                            }
                        }
                        else
                        {
                            var xmResp = responseMsgSet.ToXMLString();
                            var msg = PQExtensions.GetXmlNodeValue(xmResp);
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Error,
                                    $"{personFullName} | Invoice.Num = {invoice.Number} | {msg}"));
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"{personFullName} | Id = {invoice.ReportData?.PersonId} Customer not found."));
                    }

                    OnSyncProgressChanged?.Invoke(this, new ProgressArgs(index));
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            throw;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }


    #region INVOICES

    public async Task<List<QbInvoice>> GetAllExistingInvoicesAsync()
    {
        var sessionManager = new QBSessionManager();

        try
        {
            AllExistingInvoicesList.Clear();

            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _invoiceBuilder.BuildGetAllInvoicesRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                //var xmResp = responseMsgSet.ToXMLString();
                ReadFetchedInvoices(responseMsgSet);
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));

            return AllExistingInvoicesList;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return AllExistingInvoicesList;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private void ReadFetchedInvoices(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return;

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtInvoiceQueryRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var retList = (IInvoiceRetList)response.Detail;

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesInvoice(retList.GetAt(x));
                        }
                    }
                }
            }
        }
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

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtInvoiceAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
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

            var customer = new QbInvoice();
            customer.PopInvoiceId = Convert.ToInt32(ret.PONumber.GetValue()); //unique inv id from populi
            customer.PopInvoiceNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            customer.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            customer.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingInvoicesList.Add(customer);
            return customer;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }

    #endregion

    #region MEMOS

    public async Task<List<QbInvoice>> GetAllExistingMemosAsync()
    {
        var sessionManager = new QBSessionManager();

        try
        {
            AllExistingInvoicesList.Clear();

            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _memoBuilder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                //var xmResp = responseMsgSet.ToXMLString();
                ReadFetchedMemos(responseMsgSet);
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));

            return AllExistingInvoicesList;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return AllExistingInvoicesList;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private void ReadFetchedMemos(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return;

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCreditMemoQueryRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var retList = (ICreditMemoRetList)response.Detail;

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesMemo(retList.GetAt(x));
                        }
                    }
                }
            }
        }
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

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCreditMemoAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
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
            memo.PopInvoiceNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            memo.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            memo.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingMemosList.Add(memo);
            return memo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }

    #endregion


    #region Payments

    public async Task<List<QbInvoice>> GetAllExistingPaymentsAsync()
    {
        var sessionManager = new QBSessionManager();

        try
        {
            AllExistingPaymentsList.Clear();

            sessionManager.OpenConnection(QBCompanyService.AppId, QBCompanyService.AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _paymentBuilder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                var xmResp = responseMsgSet.ToXMLString();
                ReadFetchedPayments(responseMsgSet);
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));

            return AllExistingInvoicesList;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return AllExistingInvoicesList;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private void ReadFetchedPayments(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return;

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtReceivePaymentQueryRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var retList = (IReceivePaymentRetList)response.Detail;

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesPayments(retList.GetAt(x));
                        }
                    }
                }
            }
        }
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

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtReceivePaymentAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
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
            return payment;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }

    #endregion
}