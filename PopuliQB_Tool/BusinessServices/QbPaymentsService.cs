using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbPaymentsService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly QbItemService _itemService;
    private readonly QbCustomerService _customerService;
    private readonly QbAccountsService _accountsService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QBInvoiceService _qbInvoiceService;
    private readonly PopPaymentToQbPaymentBuilder _paymentBuilder;
    private readonly PopCreditMemoToQbCreditMemoBuilder _memoBuilder;
    private readonly PopDepositToQbDepositBuilder _depositBuilder;
    private readonly PopRefundToQbChequeBuilder _chequeBuilder;

    public List<QbMemo> AllExistingMemosList { get; set; } = new();
    public List<QbPayment> AllExistingPaymentsList { get; set; } = new();
    public List<QbCheque> AllExistingChequesList { get; set; } = new();
    public List<QbDeposit> AllExistingDepositsList { get; set; } = new();

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }

    public QbPaymentsService(
        PopPaymentToQbPaymentBuilder paymentBuilder,
        PopCreditMemoToQbCreditMemoBuilder memoBuilder,
        PopDepositToQbDepositBuilder depositBuilder,
        PopRefundToQbChequeBuilder chequeBuilder,
        PopuliAccessService populiAccessService,
        QBInvoiceService qbInvoiceService,
        QbItemService itemService,
        QbCustomerService customerService,
        QbAccountsService accountsService
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
    }


    #region PAYMENTS

    public async Task SyncPaymentsAsync()
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
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;


                List<PopPerson> allPersons;
                if (QbSettings.Instance.ApplyStudentFilter && QbSettings.Instance.Student != null)
                {
                    allPersons = new List<PopPerson> { QbSettings.Instance.Student };
                }
                else
                {
                    allPersons = _populiAccessService.AllPopuliPersons;
                }

                foreach (var person in allPersons)
                {
                    var qbCustomer =
                        _customerService.AllExistingCustomersList.First(x => x.PopPersonId == person.Id!.Value);

                    var popPaymentsAndCredits = await _populiAccessService.GetAllStudentPaymentsAsync(person.Id!.Value);

                    //-------------------------------PAYMENTS------------------------------------------------------
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing payments for student: {person.DisplayName}."));

                    var paymentsOnly =
                        popPaymentsAndCredits
                            .Where(x => x is { PaidByType: "person", RefundSource: null, AidTypeId: null }).ToList();

                    if (paymentsOnly.Any())
                    {
                        foreach (var payment in paymentsOnly)
                        {
                            var trans = await _populiAccessService.GetTransactionWithLedgerAsync(payment.TransactionId!
                                .Value);

                            if (trans == null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"Transaction not found for Payment Number: {payment.Number}"));
                                continue;
                            }

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            if (QbSettings.Instance.ApplyAddedDateFilter
                                && (trans.AddedAt!.Value.Date < QbSettings.Instance.AddedFrom.Date
                                    || trans.AddedAt!.Value.Date > QbSettings.Instance.AddedTo.Date)
                               )
                            {
                                continue;
                            }


                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"Adding payment no: {payment.Number}."));

                            var existingPay =
                                AllExistingPaymentsList.FirstOrDefault(
                                    x => x.PopPaymentNumber == payment.Number 
                                         && x.QbCustomerListId == qbCustomer.QbListId);
                            if (existingPay != null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Payment: Payment.Num: {payment.Number} already exists for: {person.DisplayName}"));

                                continue;
                            }

                            if (payment.ConvenienceFeeAmount is > 0)
                            {
                                var convEntries = trans.LedgerEntries.Where(x => x.AccountId == 74).ToList(); //conv acc
                                var fromAccIdConv= convEntries.First(x => x.Direction == "credit").AccountId!;
                                var adAccIdConv = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

                                var fromQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == fromAccIdConv).QbAccountListId;
                                var adQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAccIdConv).QbAccountListId;

                                if (convEntries.Any())
                                {
                                    var conv = new PopCredit
                                    {
                                        Id = payment.Id,
                                        Number = payment.Number,
                                        TransactionId = trans.Id,
                                        PostedOn = trans.PostedOn,
                                        Object = "deposit",
                                        ActorType = "person",
                                        ActorId = person.Id!,
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

                                    _depositBuilder.BuildAddRequest(requestMsgSet, conv, qbCustomer.QbListId!,
                                        fromQbAccListIdConv!, adQbAccListIdConv!, trans.PostedOn!.Value!);

                                    var responseMsgSetDeposit = sessionManager.DoRequests(requestMsgSet);
                                    if (!ReadAddedDeposit(responseMsgSetDeposit))
                                    {
                                        var xmResp = responseMsgSetDeposit.ToXMLString();
                                        var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                                    }
                                    else
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Success,
                                                $"Added Deposit.Num: {payment.Number} for {person.DisplayName}"));
                                    }
                                }
                            }

                            var arAccId = trans.LedgerEntries.First(x => x.Direction == "credit").AccountId!;
                            var adAccId = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

                            var arQbAccListId = _populiAccessService.AllPopuliAccounts
                                .First(x => x.Id == arAccId).QbAccountListId;
                            var adQbAccListId = _populiAccessService.AllPopuliAccounts
                                .First(x => x.Id == adAccId).QbAccountListId;

                            _paymentBuilder.BuildAddRequest(requestMsgSet, payment, qbCustomer.QbListId!,
                                arQbAccListId!, adQbAccListId!, trans.PostedOn!.Value!);

                            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                            if (!ReadAddedPayments(responseMsgSet))
                            {
                                var xmResp = responseMsgSet.ToXMLString();
                                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                            }
                            else
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Success,
                                        $"Added Payment.Num: {payment.Number} for {person.DisplayName}"));
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"No payments found for student: {person.DisplayName}."));
                    }


                    //-------------------------------CREDIT MEMOS------------------------------------------------------
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing credit memos for student: {person.DisplayName}."));

                    //SetUp Student AID Awards
                    var aidAwardsList =
                        await _populiAccessService.GetAllStudentAwardsAsync(person.Id!.Value, person.DisplayName!);
                    
                    var creditPaymentsOnly = popPaymentsAndCredits.Where(x =>
                        x is { PaidByType: "aid_provider", RefundSource: null, AidTypeId: not null } && x.AidTypeId != 47178).ToList();

                    if (creditPaymentsOnly.Any())
                    {
                        foreach (var creditPayment in creditPaymentsOnly.ToList())
                        {
                            var trans =
                                await _populiAccessService
                                    .GetTransactionWithLedgerAsync(creditPayment.TransactionId!.Value);

                            if (trans == null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"Transaction not found for credit payment no: {creditPayment.Number}"));
                                continue;
                            }

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            if (QbSettings.Instance.ApplyAddedDateFilter
                                && (trans.AddedAt!.Value.Date < QbSettings.Instance.AddedFrom.Date
                                    || trans.AddedAt!.Value.Date > QbSettings.Instance.AddedTo.Date)
                               )
                            {
                                continue;
                            }


                            var aid = aidAwardsList.First(x => x.AidTypeId == creditPayment.AidTypeId!.Value);
                            var memo = new PopCredit
                            {
                                Id = creditPayment.Id,
                                Number = creditPayment.Number,
                                TransactionId = creditPayment.TransactionId,
                                PostedOn = trans.PostedOn,
                                Object = "credit",
                                ActorType = "person",
                                ActorId = person.Id!,
                                Amount = creditPayment.Amount,
                                Items = new List<PopItem>
                                {
                                    new()
                                    {
                                        Name = aid.ReportData!.AidName,
                                        Id = aid.Id,
                                        ItemType = "aid_provider",
                                        Amount = creditPayment.Amount,
                                        Description = "",
                                        //InvoiceId = invoice.Id,
                                        ItemId = aid.AidTypeId,
                                        Object = "invoice_item",
                                    }
                                },
                            };

                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"Converted credit payment to memo no: {memo.Number}."));
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info, $"Adding memo no: {memo.Number}."));

                            var existingMemo =
                                AllExistingMemosList.FirstOrDefault(x => x.PopMemoNumber == memo.Number 
                                                                         && x.QbCustomerListId == qbCustomer.QbListId);
                            if (existingMemo != null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped Memo: Memo.Num: {existingMemo.PopMemoNumber} already exists for: {person.DisplayName}"));

                                continue;
                            }

                            if (creditPayment.ConvenienceFeeAmount is > 0)
                            {
                                var convEntries = trans.LedgerEntries.Where(x => x.AccountId == 74).ToList(); //conv acc
                                var fromAccIdConv= convEntries.First(x => x.Direction == "credit").AccountId!;
                                var adAccIdConv = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

                                var fromQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == fromAccIdConv).QbAccountListId;
                                var adQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAccIdConv).QbAccountListId;

                                if (convEntries.Any())
                                {
                                    var conv = new PopCredit
                                    {
                                        Id = creditPayment.Id,
                                        Number = creditPayment.Number,
                                        TransactionId = trans.Id,
                                        PostedOn = trans.PostedOn,
                                        Object = "deposit",
                                        ActorType = "person",
                                        ActorId = person.Id!,
                                        Amount = creditPayment.ConvenienceFeeAmount,
                                        Items = new List<PopItem>
                                        {
                                            new()
                                            {
                                                Amount = creditPayment.ConvenienceFeeAmount,
                                                Description = "",
                                            }
                                        },
                                    };

                                    _depositBuilder.BuildAddRequest(requestMsgSet, conv, qbCustomer.QbListId!,
                                        fromQbAccListIdConv!, adQbAccListIdConv!, trans.PostedOn!.Value!);

                                    var responseMsgSetDeposit = sessionManager.DoRequests(requestMsgSet);
                                    if (!ReadAddedDeposit(responseMsgSetDeposit))
                                    {
                                        var xmResp = responseMsgSetDeposit.ToXMLString();
                                        var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                                    }
                                    else
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Success,
                                                $"Added Deposit.Num: {creditPayment.Number} for {person.DisplayName}"));
                                    }
                                }


                            }


                            if (memo.Items != null)
                            {
                                if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                                {
                                    var sbItems = memo.Items.Where(x => x.Name == "Starting Balance").ToList();
                                    foreach (var sbItem in sbItems)
                                    {
                                        memo.Items.Remove(sbItem);
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Warn,
                                                $"skipped: {person.DisplayName} | Memo.Num = {memo.Number} | Memo Item {sbItem.Name}."));
                                    }
                                }

                                if (!memo.Items.Any())
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Warn,
                                            $"{person.DisplayName} | Memo.Num = {memo.Number} skipped. It has no items."));
                                    continue;
                                }

                                foreach (var credItem in memo.Items)
                                {
                                    //31 is max length for Item name field in QB
                                    if (credItem.Name!.Length > 31)
                                    {
                                        var name = credItem.Name.Substring(0, 31).Trim();
                                        credItem.Name = name.RemoveInvalidUnicodeCharacters();
                                    }

                                    var existingItem = _itemService.AllExistingItemsList.FirstOrDefault(x =>
                                        x.QbItemName!.ToLower().Trim() == credItem.Name.ToLower().Trim());
                                    if (existingItem == null)
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error,
                                                $"CreditMemo.Num = {memo.Number} | Memo Item {credItem.Name} doesn't exist in QB."));

                                        continue;
                                    }

                                    credItem.ItemQbListId = existingItem!.QbListId;
                                }
                            }


                            var arAccId = trans.LedgerEntries.First(x => x.Direction == "credit").AccountId!;
                            var arQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == arAccId)
                                .QbAccountListId;

                            _memoBuilder.BuildAddRequest(requestMsgSet, memo, qbCustomer.QbListId!, arQbAccListId!);

                            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                            if (!ReadAddedMemo(responseMsgSet))
                            {
                                var xmResp = responseMsgSet.ToXMLString();
                                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                            }
                            else
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Success,
                                        $"Added Memo.Num = {memo.Number}"));
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"No Credit memos found for student: {person.DisplayName}."));
                    }

                    //-------------------------------REFUNDS RETURNS------------------------------------------------------
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing refund returns for student: {person.DisplayName}."));

                    var refundsReturnsOnly = popPaymentsAndCredits.Where(x =>
                        x is { PaidByType: "aid_provider", RefundSource: null, AidTypeId: 47178 }).ToList();

                    if (refundsReturnsOnly.Any())
                    {
                        var invoicesList = new List<PopInvoice>();
                        foreach (var refundReturn in refundsReturnsOnly)
                        {
                            var trans =
                                await _populiAccessService
                                    .GetTransactionWithLedgerAsync(refundReturn.TransactionId!.Value);

                            if (trans == null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"Transaction not found for refundReturn no: {refundReturn.Number}"));
                                continue;
                            }

                            var item = aidAwardsList.First(x => x.AidTypeId == refundReturn.AidTypeId).ReportData!.AidName!;
                            var popInvoice = new PopInvoice
                            {
                                PostedOn = trans.PostedOn.ToString(),
                                ActorId = refundReturn.StudentId,
                                TransactionId = trans.Id,
                                Number = refundReturn.Number,
                                Description = "refund return converted to invoice.",
                                Items = new List<PopItem>
                                {
                                    new PopItem
                                    {
                                        Description = item,
                                        Amount = refundReturn.Amount,
                                        Name = item,
                                    }
                                }
                            };
                        }

                        await _qbInvoiceService.AddInvoicesAsync(invoicesList);

                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"No refund returns found for student: {person.DisplayName}."));
                    }
                    

                    //-------------------------------REFUNDS------------------------------------------------------
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Info,
                            $"Syncing refunds for student: {person.DisplayName}."));

                    var refundsOnly = popPaymentsAndCredits.Where(x =>
                        x is { PaidByType: "person", RefundSource: not null, AidTypeId: not null }).ToList();

                    if (refundsOnly.Any())
                    {
                        foreach (var refund in refundsOnly.ToList())
                        {
                            var trans =
                                await _populiAccessService
                                    .GetTransactionWithLedgerAsync(refund.TransactionId!.Value);

                            if (trans == null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error,
                                        $"Transaction not found for refund no: {refund.Number}"));
                                continue;
                            }

                            if (QbSettings.Instance.ApplyPostedDateFilter
                                && (trans.PostedOn!.Value.Date < QbSettings.Instance.PostedFrom.Date
                                    || trans.PostedOn!.Value.Date > QbSettings.Instance.PostedTo.Date)
                               )
                            {
                                continue;
                            }

                            if (QbSettings.Instance.ApplyAddedDateFilter
                                && (trans.AddedAt!.Value.Date < QbSettings.Instance.AddedFrom.Date
                                    || trans.AddedAt!.Value.Date > QbSettings.Instance.AddedTo.Date)
                               )
                            {
                                continue;
                            }


                            var aid = aidAwardsList.First(x => x.AidTypeId == refund.AidTypeId!.Value);
                            var refundCheque = new PopCredit
                            {
                                Id = refund.Id,
                                Number = refund.Number,
                                TransactionId = refund.TransactionId,
                                PostedOn = trans.PostedOn,
                                Object = "payment",
                                ActorType = "person",
                                ActorId = person.Id!,
                                Amount = refund.Amount,
                                Items = new List<PopItem>
                                {
                                    new()
                                    {
                                        Name = aid.ReportData!.AidName,
                                        Id = aid.Id,
                                        ItemType = "aid_provider",
                                        Amount = refund.Amount,
                                        Description = "",
                                        //InvoiceId = invoice.Id,
                                        ItemId = aid.AidTypeId,
                                        Object = "invoice_item",
                                    }
                                },
                            };

                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info,
                                    $"Converted payment to refund no: {refundCheque.Number}."));
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Info, $"Adding refund no: {refundCheque.Number}."));

                            var existingCheque =
                                AllExistingChequesList.FirstOrDefault(x => x.PopChequeNumber == refundCheque.Number 
                                                                           && x.QbCustomerListId == qbCustomer.QbListId);
                            if (existingCheque != null)
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Warn,
                                        $"Skipped refund: refund.Num: {existingCheque.PopChequeNumber} already exists for: {person.DisplayName}"));

                                continue;
                            }


                            if (refund.ConvenienceFeeAmount is > 0)
                            {
                                var convEntries = trans.LedgerEntries.Where(x => x.AccountId == 74).ToList(); //conv acc
                                var fromAccIdConv= convEntries.First(x => x.Direction == "credit").AccountId!;
                                var adAccIdConv = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;

                                var fromQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == fromAccIdConv).QbAccountListId;
                                var adQbAccListIdConv = _populiAccessService.AllPopuliAccounts.First(x => x.Id == adAccIdConv).QbAccountListId;

                                if (convEntries.Any())
                                {
                                    var conv = new PopCredit
                                    {
                                        Id = refund.Id,
                                        Number = refund.Number,
                                        TransactionId = trans.Id,
                                        PostedOn = trans.PostedOn,
                                        Object = "deposit",
                                        ActorType = "person",
                                        ActorId = person.Id!,
                                        Amount = refund.ConvenienceFeeAmount,
                                        Items = new List<PopItem>
                                        {
                                            new()
                                            {
                                                Amount = refund.ConvenienceFeeAmount,
                                                Description = "",
                                            }
                                        },
                                    };

                                    _depositBuilder.BuildAddRequest(requestMsgSet, conv, qbCustomer.QbListId!,
                                        fromQbAccListIdConv!, adQbAccListIdConv!, trans.PostedOn!.Value!);

                                    var responseMsgSetDeposit = sessionManager.DoRequests(requestMsgSet);
                                    if (!ReadAddedDeposit(responseMsgSetDeposit))
                                    {
                                        var xmResp = responseMsgSetDeposit.ToXMLString();
                                        var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                                    }
                                    else
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Success,
                                                $"Added Deposit.Num: {refund.Number} for {person.DisplayName}"));
                                    }
                                }


                            }



                            if (refundCheque.Items != null)
                            {
                                if (QbSettings.Instance.ApplyIgnoreStartingBalanceFilter)
                                {
                                    var sbItems = refundCheque.Items.Where(x => x.Name == "Starting Balance").ToList();
                                    foreach (var sbItem in sbItems)
                                    {
                                        refundCheque.Items.Remove(sbItem);
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Warn,
                                                $"Skipped: refund.Num = {refundCheque.Number} | refund Item {sbItem.Name}."));
                                    }
                                }

                                if (!refundCheque.Items.Any())
                                {
                                    OnSyncStatusChanged?.Invoke(this,
                                        new StatusMessageArgs(StatusMessageType.Warn,
                                            $"Skipped: refund.Num = {refundCheque.Number}. It has no items."));
                                    continue;
                                }

                                foreach (var item in refundCheque.Items)
                                {
                                    //31 is max length for Item name field in QB
                                    if (item.Name!.Length > 31)
                                    {
                                        var name = item.Name.Substring(0, 31).Trim();
                                        item.Name = name.RemoveInvalidUnicodeCharacters();
                                    }

                                    var existingItem = _itemService.AllExistingItemsList.FirstOrDefault(x =>
                                        x.QbItemName!.ToLower().Trim() ==
                                        item.Name.ToLower().Trim());
                                    if (existingItem == null)
                                    {
                                        OnSyncStatusChanged?.Invoke(this,
                                            new StatusMessageArgs(StatusMessageType.Error,
                                                $"refund.Num = {refundCheque.Number} | refund Item {item.Name} doesn't exist in QB."));

                                        continue;
                                    }

                                    item.ItemQbListId = existingItem!.QbListId;
                                }
                            }


                            var bankAccId = trans.LedgerEntries.First(x => x.Direction == "credit").AccountId!;
                            var recAccId = trans.LedgerEntries.First(x => x.Direction == "debit").AccountId!;
                            var bankQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == bankAccId).QbAccountListId;
                            var recQbAccListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == recAccId).QbAccountListId;

                            _chequeBuilder.BuildAddRequest(requestMsgSet, refundCheque, qbCustomer.QbListId!, bankQbAccListId!, recQbAccListId!, trans.PostedOn!.Value);

                            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                            if (!ReadAddedCheque(responseMsgSet))
                            {
                                var xmResp = responseMsgSet.ToXMLString();
                                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                            }
                            else
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Success,
                                        $"Added refund.Num = {refundCheque.Number}"));
                            }
                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info,
                                $"No refunds found for student: {person.DisplayName}."));
                    }

                }

                OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
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

    public async Task SyncAllExistingPaymentsAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            AllExistingPaymentsList.Clear();

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

                _paymentBuilder.BuildGetAllRequest(requestMsgSet);
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
            payment.PopPaymentNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            payment.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            payment.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingPaymentsList.Add(payment);
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


    #region MEMOS

    public async Task SyncAllExistingMemosAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            AllExistingMemosList.Clear();

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

                _memoBuilder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedMemos(responseMsgSet))
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
                    $"Completed: Found Memos in QB {AllExistingMemosList.Count}"));
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

    private bool ReadFetchedMemos(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCreditMemoQueryRs)
                    {
                        var retList = (ICreditMemoRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesMemo(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
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

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCreditMemoAddRs)
                    {
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
            memo.PopMemoNumber = Convert.ToInt32(ret.RefNumber.GetValue());
            memo.QbCustomerListId = ret.CustomerRef.ListID.GetValue();
            memo.QbCustomerName = ret.CustomerRef.FullName.GetValue();

            AllExistingMemosList.Add(memo);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Memo: {memo.PopMemoNumber}"));
            return memo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{ex.Message}"));
        }

        return null;
    }

    #endregion

    #region CHEQUES

    public async Task SyncAllExistingChequesAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;
        try
        {
            AllExistingChequesList.Clear();

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

                _chequeBuilder.BuildGetAllRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedCheques(responseMsgSet))
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
                    $"Completed: Found Cheques in QB {AllExistingChequesList.Count}"));
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

    private bool ReadFetchedCheques(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCheckQueryRs)
                    {
                        var retList = (ICheckRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesCheque(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool ReadAddedCheque(IMsgSetResponse? responseMsgSet)
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
                    if (responseType == ENResponseType.rtCheckAddRs)
                    {
                        var ret = (ICheckRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesCheque(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    private QbCheque? ReadPropertiesCheque(ICheckRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var cheque = new QbCheque();
            cheque.QbCustomerListId = ret.PayeeEntityRef.ListID.GetValue();
            cheque.QbCustomerName = ret.PayeeEntityRef.FullName.GetValue();

            var refNum = ret.Memo.GetValue();
            if (!string.IsNullOrEmpty(refNum))
            {
                var arr = refNum.Split("#");
                cheque.PopChequeNumber = Convert.ToInt32(arr[1].Trim());
            }

            AllExistingChequesList.Add(cheque);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found Cheque: {cheque.PopChequeNumber}"));
            return cheque;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{ex.Message}"));
        }

        return null;
    }

    #endregion

    
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

                _depositBuilder.BuildGetAllRequest(requestMsgSet);
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