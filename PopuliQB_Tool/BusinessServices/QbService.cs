using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbService
{
    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();


    private readonly QbInvoiceServiceQuick _invoiceServiceQuick;
    private readonly QbPaymentServiceQuick _paymentServiceQuick;
    private readonly QbCreditMemoServiceQuick _creditMemoServiceQuick;
    private readonly QbRefundServiceQuick _refundServiceQuick;
    private readonly PopuliAccessService _populiAccessService;

    public QbService(
        PopuliAccessService populiAccessService,
        QbInvoiceServiceQuick invoiceServiceQuick,
        QbPaymentServiceQuick paymentServiceQuick,
        QbCreditMemoServiceQuick creditMemoServiceQuick,
        QbRefundServiceQuick refundServiceQuick
    )
    {
        _populiAccessService = populiAccessService;
        _invoiceServiceQuick = invoiceServiceQuick;
        _paymentServiceQuick = paymentServiceQuick;
        _creditMemoServiceQuick = creditMemoServiceQuick;
        _refundServiceQuick = refundServiceQuick;
    }


    public async Task SyncAllPaymentsAndMemos()
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
                            $"Syncing Payments & Memos for student: {person.DisplayName}."));

                    var allPayments = await _populiAccessService.GetAllStudentPaymentsAsync(person.Id!.Value!);

                    if (allPayments.Any())
                    {
                        foreach (var payment in allPayments)
                        {
                            await Task.Delay(2000);
                            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(
                                payment.TransactionId!
                                    .Value);

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            switch (trans.Type)
                            {
                                case "aid_payment":
                                    var respAid =
                                        await _creditMemoServiceQuick.AddCreditMemo(person, trans, payment,
                                            sessionManager);
                                    break;
                                case "customer_payment":
                                    var respPay =
                                        _paymentServiceQuick.AddPaymentAsync(person, trans, payment, sessionManager);
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
                                $"Payments & Memos not found for student: {person.DisplayName} in Populi."));
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
                            $"Syncing Refunds for student: {person.DisplayName}."));


                    var allRefunds =
                        await _populiAccessService.GetAllStudentRefundsAsync(person.Id!.Value, person.DisplayName!);

                    if (allRefunds.Any())
                    {
                        foreach (var refund in allRefunds)
                        {
                            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(
                                refund.TransactionId!
                                    .Value);

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            switch (refund.Type)
                            {
                                case "refund_to_student":
                                    var respRefund =
                                        _refundServiceQuick.AddRefund(person, trans, refund, sessionManager);
                                    break;
                                case "refund_to_source":
                                    var respRefundToSource =
                                        _invoiceServiceQuick.AddInvoiceForRefundToSourceAsync(person, trans,
                                            refund, sessionManager);
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
                                $"Refunds not found for student: {person.DisplayName} in Populi."));
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

    public async Task SyncAllInvoicesAndSaleCredits()
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
                            $"Syncing Invoices & Sale Credits for student: {person.DisplayName}."));

                    var allInvoices =
                        await _populiAccessService.GetAllStudentInvoicesAsync(person.Id!.Value, person.DisplayName!);

                    if (allInvoices.Any())
                    {
                        foreach (var invoice in allInvoices)
                        {
                            var trans =
                                await _populiAccessService.GetTransactionByIdWithLedgerAsync(invoice.TransactionId!
                                    .Value);

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            var respInv =
                                await _invoiceServiceQuick.AddInvoiceAsync(person, trans, invoice, sessionManager);
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Invoices not found for student: {person.DisplayName} in Populi."));
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