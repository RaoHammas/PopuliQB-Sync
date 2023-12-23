using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbAccountsService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopAccountsToQbAccountsBuilder _builder;
    public List<QbAccount> AllExistingAccountsList { get; set; } = new();

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }

    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }

    public QbAccountsService(PopAccountsToQbAccountsBuilder builder)
    {
        _builder = builder;
    }


    public async Task<List<QbAccount>> GetAllExistingAccountsAsync()
    {
        AllExistingAccountsList.Clear();
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
                _builder.BuildGetAllFromQbRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                if (!ReadFetchedAccounts(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PQExtensions.GetXmlNodeValue(xmResp);
                    OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));
            return AllExistingAccountsList;
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


    private bool ReadFetchedAccounts(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtAccountQueryRs)
                    {
                        var retList = (IAccountRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));
                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesAccount(retList.GetAt(x));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }


    private QbAccount? ReadPropertiesAccount(IAccountRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var acc = new QbAccount();
            acc.FullName = ret.FullName.GetValue();
            if (ret.AccountNumber != null)
            {
                acc.Number = ret.AccountNumber.GetValue();
            }

            acc.Title = ret.Name.GetValue();
            acc.ListId = ret.ListID.GetValue();
            
            AllExistingAccountsList.Add(acc);

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, $"Found: {acc.Title}"));
            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
            return acc;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }
}