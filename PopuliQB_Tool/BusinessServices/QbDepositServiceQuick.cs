using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbDepositServiceQuick
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopuliAccessService _populiAccessService;
    private readonly PopDepositToQbDepositBuilder _builder;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbDeposit> AllExistingDepositsList { get; set; } = new();


    public QbDepositServiceQuick(
        PopDepositToQbDepositBuilder builder,
        PopuliAccessService populiAccessService,
        PopDepositToQbDepositBuilder depositBuilder
    )
    {
        _builder = builder;
        _populiAccessService = populiAccessService;
        _builder = depositBuilder;
    }


    public bool AddDeposit(PopPayment payment, QBCustomer qbStudent, int arAccId, int adAcc, DateTime? transPostedOn,
        QBSessionManager sessionManager)
    {
        try
        {
            /*var existing = AllExistingDepositsList.FirstOrDefault(x => x.UniqueId == key);
            if (existing != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped: Deposit: {payment.Number} already exists as Deposit.Num: {payment.Number}."));
                return false;
            }*/

            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var fromQbAccListIdConv =
                _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;
            var adQbAccListIdConv =
                _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAcc).QbAccountListId;

            var conv = new PopCredit
            {
                Id = payment.Id,
                TransactionId = payment.TransactionId,
                PostedOn = transPostedOn!.Value,
                Object = "deposit",
                ActorType = "person",
                ActorId = payment.StudentId!,
                Amount = payment.ConvenienceFeeAmount,
                Items = new List<PopItem>
                {
                    new()
                    {
                        Amount = payment.ConvenienceFeeAmount,
                        Description = "",
                    }
                },
            };

            _builder.BuildAddRequest(requestMsgSet, conv, qbStudent.QbListId!, fromQbAccListIdConv!, adQbAccListIdConv!,
                transPostedOn!.Value!);
            var responseMsgSetDeposit = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedDeposit(responseMsgSetDeposit))
            {
                var xmResp = responseMsgSetDeposit.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));

                return false;
            }

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Added Deposit.Num: {payment.Number} for {qbStudent.QbCustomerName}"));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));

            return false;
        }
    }


    #region DEPOSITS

    public async Task SyncAllExistingDepositsAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            AllExistingDepositsList.Clear();

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
                if (!ReadFetchedDeposits(responseMsgSet))
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
                    $"Completed: Found Deposits in QB {AllExistingDepositsList.Count}"));
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

    private bool ReadFetchedDeposits(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtDepositQueryRs)
                    {
                        var retList = (IDepositRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesDeposit(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedDeposit(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtDepositAddRs)
                    {
                        var ret = (IDepositRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesDeposit(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbDeposit? ReadPropertiesDeposit(IDepositRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var deposit = new QbDeposit();
            
            //AllExistingDepositsList.Add(deposit);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Deposit."));
            return deposit;
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