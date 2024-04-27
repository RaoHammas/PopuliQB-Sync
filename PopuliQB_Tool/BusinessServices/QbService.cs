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
    private readonly QbJournalServiceQuick _qbJournalServiceQuick;
    private readonly PopuliAccessService _populiAccessService;

    public QbService(
        PopuliAccessService populiAccessService,
        QbInvoiceServiceQuick invoiceServiceQuick,
        QbPaymentServiceQuick paymentServiceQuick,
        QbCreditMemoServiceQuick creditMemoServiceQuick,
        QbRefundServiceQuick refundServiceQuick,
        QbJournalServiceQuick qbJournalServiceQuick
    )
    {
        _populiAccessService = populiAccessService;
        _invoiceServiceQuick = invoiceServiceQuick;
        _paymentServiceQuick = paymentServiceQuick;
        _creditMemoServiceQuick = creditMemoServiceQuick;
        _refundServiceQuick = refundServiceQuick;
        _qbJournalServiceQuick = qbJournalServiceQuick;
    }


    public async Task SyncAllPaymentsAndMemos()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
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
                            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(
                                payment.TransactionId!
                                    .Value);
                            if (trans.Id is null or < 1)
                            {
                                /*OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Payment.Number {payment.Number}. Transaction.Id {payment.TransactionId} is not found for it. for student: {person.DisplayName!}. Is it Void?"));*/
                                _logger.Warn($"Skipped Payment.Number {payment.Number}. Transaction.Id {payment.TransactionId} is not found for it. for student: {person.DisplayName!}. Is it Void?");
                                continue;
                            }

                            if (QbSettings.Instance.ApplyNumFilter
                                && (trans.Number!.Value < Convert.ToInt32(QbSettings.Instance.NumFrom)
                                    || trans.Number!.Value > Convert.ToInt32(QbSettings.Instance.NumTo))
                               )
                            {
                                continue;
                            }

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
                                    await _creditMemoServiceQuick.AddCreditMemo(person, trans, payment,
                                        sessionManager);
                                  
                                    break;
                                case "customer_payment":
                                    _paymentServiceQuick.AddPaymentAsync(person, trans, payment, sessionManager);
                                    break;
                                default:
                                    break;
                            }

                            if (payment.RefundSource != null)
                            {
                                var refund = await _populiAccessService.GetCustomerRefundByPaymentIdAsync(payment.Id!.Value);
                                var transRef = await _populiAccessService.GetTransactionByIdWithLedgerAsync(refund.TransactionId!.Value);

                                _refundServiceQuick.AddCustomerRefund(person, transRef, refund, sessionManager);
                            }

                            if (trans.ReversedById != null)
                            {
                                var transRev = await _populiAccessService.GetTransactionByIdWithLedgerAsync(trans.ReversedById!.Value);

                                if (payment.AidTypeId == null && trans.Type == "customer_payment")
                                {
                                    _qbJournalServiceQuick.AddJournalEntry(person, transRev, Convert.ToInt32(payment.Number), sessionManager);
                                }
                                else
                                {
                                    var aid = await _populiAccessService.GetAidTypeByIdAsync(payment.AidTypeId!.Value);

                                    if (aid.IsScholarship is true)
                                    {
                                        var respRefundToSource = _invoiceServiceQuick.AddInvoiceForPaymentAsync(person, transRev, payment, aid, sessionManager);
                                    }
                                    else
                                    {
                                        _qbJournalServiceQuick.AddJournalEntry(person, transRev, Convert.ToInt32(payment.Number), sessionManager);
                                    }
                                }
                              
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
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
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

                            if (trans.Id == null || trans.Id < 1)
                            {
                                /*OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Refund. Transaction.Id {refund.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?"));*/
                                _logger.Warn($"Skipped Refund. Transaction.Id {refund.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?");
                                continue;
                            }

                            if (QbSettings.Instance.ApplyNumFilter
                                && (trans.Number!.Value < Convert.ToInt32(QbSettings.Instance.NumFrom)
                                    || trans.Number!.Value > Convert.ToInt32(QbSettings.Instance.NumTo))
                               )
                            {
                                continue;
                            }

                            var resp = false;
                            switch (refund.Type)
                            {
                                case "refund_to_student":
                                    resp = _refundServiceQuick.AddRefund(person, trans, refund, sessionManager);
                                    break;
                                case "refund_to_source":
                                    resp = _invoiceServiceQuick.AddInvoiceForRefundToSourceAsync(person, trans, refund, sessionManager);
                                    break;
                                default:
                                    break;
                            }

                            if (refund.Status != null && refund.Status.ToLower() == "void")
                            {
                                var transRev = await _populiAccessService.GetTransactionByIdWithLedgerAsync(trans.ReversedById!.Value);
                                _qbJournalServiceQuick.AddJournalEntry(person, transRev, refund.RefundId!.Value, sessionManager);
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
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
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
                    
                    var allCredits =
                        await _populiAccessService.GetAllStudentCreditsAsync(person.Id!.Value, person.DisplayName!);
                    if (allCredits.Any())
                    {
                        foreach (var credit in allCredits)
                        {
                            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(credit.TransactionId!.Value);
                            if (trans.Id is null or < 1)
                            {
                                /*OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Invoices.Number {invoice.Number}. Transaction.Id {invoice.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?"));*/
                                
                                _logger.Warn($"Skipped Invoices.Number {credit.Number}. Transaction.Id {credit.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?");
                                continue;
                            }

                            if (QbSettings.Instance.ApplyNumFilter
                                && (trans.Number!.Value < Convert.ToInt32(QbSettings.Instance.NumFrom)
                                    || trans.Number!.Value > Convert.ToInt32(QbSettings.Instance.NumTo))
                               )
                            {
                                continue;
                            }

                            /*if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }*/

                            var resp = await _creditMemoServiceQuick.AddCreditMemoForSalesCredit(person, credit, sessionManager);
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn,
                                $"Credits not found for student: {person.DisplayName} in Populi."));
                    }


                    var allInvoices =
                        await _populiAccessService.GetAllStudentInvoicesAsync(person.Id!.Value, person.DisplayName!);

                    if (allInvoices.Any())
                    {
                        foreach (var invoice in allInvoices)
                        {
                            var trans = await _populiAccessService.GetTransactionByIdWithLedgerAsync(invoice.TransactionId!.Value);
                            if (trans.Id is null or < 1)
                            {
                                /*OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Invoices.Number {invoice.Number}. Transaction.Id {invoice.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?"));*/
                                
                                _logger.Warn($"Skipped Invoices.Number {invoice.Number}. Transaction.Id {invoice.TransactionId} is not found for it. For student: {person.DisplayName!}. Is it Void?");
                                continue;
                            }

                            if (QbSettings.Instance.ApplyNumFilter
                                && (trans.Number!.Value < Convert.ToInt32(QbSettings.Instance.NumFrom)
                                    || trans.Number!.Value > Convert.ToInt32(QbSettings.Instance.NumTo))
                               )
                            {
                                continue;
                            }

                            /*if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }*/

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