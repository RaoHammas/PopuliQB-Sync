using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbService
{
    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly QbItemService _itemService;
    private readonly QbCustomerService _customerService;
    private readonly QbAccountsService _accountsService;
    private readonly QbInvoiceServiceQuick _invoiceServiceQuick;
    private readonly QbPaymentServiceQuick _paymentServiceQuick;
    private readonly QbCreditMemoServiceQuick _creditMemoServiceQuick;
    private readonly QbRefundServiceQuick _refundServiceQuick;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QBInvoiceService _qbInvoiceService;
    private readonly PopPaymentToQbPaymentBuilder _paymentBuilder;
    private readonly PopCreditMemoToQbCreditMemoBuilder _memoBuilder;
    private readonly PopDepositToQbDepositBuilder _depositBuilder;
    private readonly PopRefundToQbChequeBuilder _chequeBuilder;

    public QbService(
        PopPaymentToQbPaymentBuilder paymentBuilder,
        PopCreditMemoToQbCreditMemoBuilder memoBuilder,
        PopDepositToQbDepositBuilder depositBuilder,
        PopRefundToQbChequeBuilder chequeBuilder,
        PopuliAccessService populiAccessService,
        QBInvoiceService qbInvoiceService,
        QbItemService itemService,
        QbCustomerService customerService,
        QbAccountsService accountsService,
        QbInvoiceServiceQuick invoiceServiceQuick,
        QbPaymentServiceQuick paymentServiceQuick,
        QbCreditMemoServiceQuick creditMemoServiceQuick,
        QbRefundServiceQuick refundServiceQuick
    )
    {
        _paymentBuilder = paymentBuilder;
        _memoBuilder = memoBuilder;
        _depositBuilder = depositBuilder;
        _chequeBuilder = chequeBuilder;
        _populiAccessService = populiAccessService;
        _qbInvoiceService = qbInvoiceService;
        _itemService = itemService;
        _customerService = customerService;
        _accountsService = accountsService;
        _invoiceServiceQuick = invoiceServiceQuick;
        _paymentServiceQuick = paymentServiceQuick;
        _creditMemoServiceQuick = creditMemoServiceQuick;
        _refundServiceQuick = refundServiceQuick;
    }


    public async Task SyncAllPaymentsAndCredits()
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
                List<PopPerson> allPersons;
                if (QbSettings.Instance.ApplyStudentFilter && QbSettings.Instance.Student != null)
                {
                    allPersons = new List<PopPerson> { QbSettings.Instance.Student };
                }
                else
                {
                    allPersons = _populiAccessService.AllPopuliPersons;
                }

                OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, allPersons.Count));

                foreach (var person in allPersons)
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing Transactions for student: {person.DisplayName}."));

                    /*var allTransactionsOfStd =
                        await _populiAccessService.GetAllStudentTransactionsAsync(person.Id!.Value,
                            person.DisplayName!);*/

                    var allPayments = await _populiAccessService.GetAllStudentPaymentsAsync(person.Id.Value!);

                    if (allPayments.Any())
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"{allPayments.Count} Payments found for student: {person.DisplayName} in Populi."));
                        foreach (var payment in allPayments)
                        {
                            var trans = await _populiAccessService.GetTransactionWithLedgerAsync(payment.TransactionId!
                                .Value);

                            switch (trans.Type)
                            {
                                case "aid_payment":
                                    var respAid = await _creditMemoServiceQuick.AddCreditMemo(person, trans, sessionManager);
                                    break;
                                /*case "sales_invoice":
                                    var respInv =
                                        await _invoiceServiceQuick.AddInvoiceAsync(person, trans, sessionManager);
                                    break;*/
                                /*case "aid_repayment":
                                    if (payment.RefundSource != null)
                                    {
                                        
                                    }
                                    else
                                    {
                                        
                                    }
                                    break;*/
                                case "customer_payment":
                                    var respPay = await _paymentServiceQuick.AddPaymentAsync(person, trans, sessionManager);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Transactions not found for student: {person.DisplayName} in Populi."));
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
    
    
    public async Task SyncAllRefunds()
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
                List<PopPerson> allPersons;
                if (QbSettings.Instance.ApplyStudentFilter && QbSettings.Instance.Student != null)
                {
                    allPersons = new List<PopPerson> { QbSettings.Instance.Student };
                }
                else
                {
                    allPersons = _populiAccessService.AllPopuliPersons;
                }

                OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, allPersons.Count));

                foreach (var person in allPersons)
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing Transactions for student: {person.DisplayName}."));


                    var allRefunds =
                        await _populiAccessService.GetAllStudentRefundsAsync(person.Id!.Value, person.DisplayName!);

                    if (allRefunds.Any())
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"{allRefunds.Count} Refunds found for student: {person.DisplayName} in Populi."));
                        foreach (var refund in allRefunds)
                        {
                            var trans = await _populiAccessService.GetTransactionWithLedgerAsync(refund.TransactionId!.Value);

                            switch (trans.Type)
                            {
                                case "refund_to_student":
                                    var respRefund = await _refundServiceQuick.AddRefund(person, trans, refund, sessionManager);
                                    break;
                                case "refund_to_source":
                                    var respPay = await _invoiceServiceQuick.AddInvoiceAsync(person, trans, sessionManager);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Transactions not found for student: {person.DisplayName} in Populi."));
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
}