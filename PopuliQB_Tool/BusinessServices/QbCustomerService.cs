using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using NLog;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;

namespace PopuliQB_Tool.BusinessServices;

public class QbCustomerService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopPersonToQbCustomerBuilder _customerBuilder;

    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }
    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QBCustomer> AllExistingCustomersList { get; set; } = new();

    public QbCustomerService(PopPersonToQbCustomerBuilder customerBuilder)
    {
        _customerBuilder = customerBuilder;
    }

    #region ADD NEW CUSTOMER

    public async Task<bool> AddCustomersAsync(List<PopPerson> persons)
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

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                foreach (var person in persons)
                {
                    if (AllExistingCustomersList.FirstOrDefault(x => x.PopPersonId == person.Id) != null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"{person.DisplayName} | Id = {person.Id} already exists."));
                        continue;
                    }

                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Adding student: {person.DisplayName} | Id = {person.Id}"));
                    _customerBuilder.BuildQbCustomerAddRequest(requestMsgSet, person);
                    var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                    if (ReadAddedCustomer(responseMsgSet))
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Success,
                                $"Added student: {person.DisplayName} | Id = {person.Id}"));
                    }
                    else
                    {
                        var xmResp = responseMsgSet.ToXMLString();
                        var msg = PqExtensions.GetXmlNodeValue(xmResp);
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"Failed to Add: {person.DisplayName} | Id = {person.Id} | {msg}"));
                    }

                    OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
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

    private bool ReadAddedCustomer(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCustomerAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var customerRet = (ICustomerRet)response.Detail;
                        if (customerRet != null)
                        {
                            ReadCustomerProperties(customerRet);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    #endregion

    #region GET ALL CUSTOMERS

    public async Task SyncAllExistingCustomersAsync()
    {
        var sessionManager = new QBSessionManager();

        try
        {
            AllExistingCustomersList.Clear();

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

                _customerBuilder.BuildGetAllQbCustomersRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedCustomers(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Error,
                            $"Error reading customer | {msg}"));
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

    private bool ReadFetchedCustomers(IMsgSetResponse? responseMsgSet)
    {
        var responseList = responseMsgSet?.ResponseList;
        if (responseList == null) return false;

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCustomerQueryRs)
                    {
                        var retList = (ICustomerRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadCustomerProperties(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private QBCustomer? ReadCustomerProperties(ICustomerRet? customerRet)
    {
        try
        {
            if (customerRet == null) return null;

            var customer = new QBCustomer();
            customer.QbListId = customerRet.ListID.GetValue();
            customer.PopPersonId = Convert.ToInt32(customerRet.Fax.GetValue());
            customer.QbCustomerName = customerRet.Name.GetValue();
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Customer: {customer.QbCustomerName}"));
            AllExistingCustomersList.Add(customer);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }

    #endregion
}