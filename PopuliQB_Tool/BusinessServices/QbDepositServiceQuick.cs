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


    public bool AddDeposit(PopTransaction trans, PopPayment payment,
        QBCustomer qbStudent,
        QBSessionManager sessionManager)
    {
        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;
            var personFullName = trans.ReportData!.PrimaryActor!;

            var convEntries = trans.LedgerEntries.Where(x => x.AccountId == QbSettings.Instance.PopConvenienceAccId).ToList(); //conv acc
            var fromAccIdConv = convEntries.First(x => x.Direction == "credit").AccountId!;
            var adAccIdConv = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

            var fromQbAccListIdConv =
                _populiAccessService.AllPopuliAccounts.First(x => x.Id == fromAccIdConv).QbAccountListId;
            var adQbAccListIdConv =
                _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAccIdConv).QbAccountListId;

            var conv = new PopCredit
            {
                Id = payment.Id,
                Number = payment.Number,
                TransactionId = trans.Id,
                PostedOn = trans.PostedOn,
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

            _builder.BuildAddRequest(requestMsgSet, conv, qbStudent.QbListId!, fromQbAccListIdConv!,
                adQbAccListIdConv!, trans.PostedOn!.Value!);
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
                    $"Added Deposit.Num: {payment.Number} for {personFullName}"));
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

            var refNum = ret.Memo.GetValue();
            if (!string.IsNullOrEmpty(refNum))
            {
                var arr = refNum.Split("#");
                deposit.PopDepositNumber = Convert.ToInt32(arr[1].Trim());
            }

            AllExistingDepositsList.Add(deposit);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Deposit: {deposit.PopDepositNumber}"));
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