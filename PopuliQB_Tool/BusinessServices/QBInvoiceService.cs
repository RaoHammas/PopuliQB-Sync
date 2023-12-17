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
    private readonly QbCustomerService _customerService;
    private readonly QBInvoiceItemService _invoiceItemService;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }
    public List<QbInvoice> AllExistingInvoicesList { get; set; } = new();

    public QBInvoiceService(
        PopInvoiceToQbInvoiceBuilder invoiceBuilder,
        QbCustomerService customerService, QBInvoiceItemService invoiceItemService)
    {
        _invoiceBuilder = invoiceBuilder;
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
                    // var personFullName = invoice.ReportData?.DisplayName;
                    var existing = _customerService
                        .AllExistingCustomersList[Random.Shared.Next(0, _customerService.AllExistingCustomersList.Count - 1)];

                    var personFullName = existing.QbCustomerName;
                    invoice.ReportData!.DisplayName = personFullName;
                    invoice.ReportData!.PersonId = existing.PopPersonId;

                    var existingCustomer =
                        _customerService.AllExistingCustomersList.FirstOrDefault(x =>
                            x.PopPersonId == invoice.ReportData?.PersonId);

                    if (existingCustomer != null)
                    {
                        foreach (var invoiceItem in invoice.Items!)
                        {
                            var existedInvItem =
                                _invoiceItemService.AllExistingInvoiceServiceItemsList.FirstOrDefault(x =>
                                    x.QbItemName == invoiceItem.Name);
                            if (existedInvItem == null)
                            {
                                await _invoiceItemService.AddInvoiceServiceItemAsync(invoiceItem, sessionManager, requestMsgSet);
                                invoiceItem.Name = _invoiceItemService.AllExistingInvoiceServiceItemsList.Last().QbItemName;
                            }
                        }

                        _invoiceBuilder.BuildInvoiceAddRequest(requestMsgSet, invoice, existingCustomer.QbListId!);
                        var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                        if (ReadAddedInvoice(responseMsgSet))
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Success,
                                    $"{personFullName} | Invoice.Num = {invoice.Number}"));
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
                            ReadProperties(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }


    #region GET ALL INVOICES

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
                            ReadProperties(retList.GetAt(x));
                        }
                    }
                }
            }
        }
    }

    private QbInvoice? ReadProperties(IInvoiceRet? ret)
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
}