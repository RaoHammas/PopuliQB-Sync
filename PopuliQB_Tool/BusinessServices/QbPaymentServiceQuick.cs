using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbPaymentServiceQuick
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopPaymentToQbPaymentBuilder _builder;
    private readonly QbCustomerService _customerService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QbDepositServiceQuick _depositServiceQuick;

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<QbPayment> AllExistingPaymentsList { get; set; } = new();


    public QbPaymentServiceQuick(
        PopPaymentToQbPaymentBuilder builder,
        QbCustomerService customerService,
        PopuliAccessService populiAccessService,
        QbDepositServiceQuick depositServiceQuick
    )
    {
        _builder = builder;
        _customerService = customerService;
        _populiAccessService = populiAccessService;
        _depositServiceQuick = depositServiceQuick;
    }


    #region PAYMENTS

    public bool AddPaymentAsync(PopPerson person, PopTransaction trans, PopPayment payment,
        QBSessionManager sessionManager)
    {
        try
        {
            var numb = payment.Number;
            
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            var qbStudent =
                _customerService.AllExistingCustomersList
                    .FirstOrDefault(x => x.QbCustomerFName == person.FirstName!.Trim() 
                                         && x.QbCustomerLName == person.LastName!.Trim());

            if (qbStudent == null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error,
                        $"Student: {person.DisplayName!} | Id: {person.Id!} not found in QB."));

                return false;
            }

            /*var existingPay =
                AllExistingPaymentsList.FirstOrDefault(
                    x => x.UniqueId == key
                         && x.QbCustomerListId == qbStudent.QbListId);
            if (existingPay != null)
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Warn,
                        $"Skipped Payment: Payment.Num: {numb} already exists for: {person.DisplayName!}"));

                return false;
            }*/

            var nonConvEntries = trans.LedgerEntries.Where(x => x.AccountId != QbSettings.Instance.PopConvenienceAccId).ToList();
            var convEntries = trans.LedgerEntries.Where(x => x.AccountId == QbSettings.Instance.PopConvenienceAccId).ToList();
            var arAccId = nonConvEntries.First(x => x.Direction == "credit").AccountId!;
            var adAccId = nonConvEntries.First(x => x.Direction == "debit").AccountId!;

            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId).QbAccountListId;
            var adQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAccId).QbAccountListId;

            _builder.BuildAddRequest(requestMsgSet, payment, qbStudent.QbListId!, arQbAccListId!, adQbAccListId!,
                trans.PostedOn!.Value!);
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
            if (!ReadAddedPayments(responseMsgSet))
            {
                var xmResp = responseMsgSet.ToXMLString();
                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));

                return false;
            }

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Added Payment.Num: {numb} for student: {person.DisplayName!}"));

            if (convEntries.Any())
            {
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info,
                        $"Adding convenience fee as Deposit for Payment.Num: {numb} for student: {person.DisplayName!}"));
                
                foreach (var convEntry in convEntries)
                {
                    var convPayment = new PopPayment
                    {
                        Id = payment.Id,
                        StudentId = person.Id,
                        ConvenienceFeeAmount = convEntry.Credit!.Value,
                        TransactionId = convEntry.TransactionId,
                    };

                    var arAcc = convEntry.AccountId!.Value;
                    var adAcc = trans.LedgerEntries.First(x => x.Direction == "debit" && x.Debit!.Value! == convEntry.Credit!.Value).AccountId!.Value;
                    
                    var resp = _depositServiceQuick.AddDeposit(convPayment, qbStudent, arAcc, adAcc, trans.PostedOn, sessionManager);
                    if (resp)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Success,
                                $"Added convenience fee as Deposit for Payment.Num: {numb} for student: {person.DisplayName!}"));
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error,
                                $"Failed to add convenience fee as Deposit for Payment.Num: {numb} for student: {person.DisplayName!}. Add manually!"));
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

    public async Task SyncAllExistingPaymentsAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            AllExistingPaymentsList.Clear();

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
                    $"Completed: Found Payments in QB: {AllExistingPaymentsList.Count}"));
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
            payment.PopPaymentNumber = ret.RefNumber.GetValue();
            payment.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            payment.QbCustomerName = ret.CustomerRef.FullName.GetValue();
            
            //AllExistingPaymentsList.Add(payment);
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