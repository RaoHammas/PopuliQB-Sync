namespace PopuliQB_Tool.BusinessObjects;

public class QbInvoice
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }
    public object? PopInvoiceNumber { get; set; }
    public int? PopInvoiceId { get; set; }
    public string UniqueId { get; set; }
}

public class QbMemo
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }
    public object? PopMemoNumber { get; set; }
    public int? PopInvoiceId { get; set; }
    public string UniqueId { get; set; }
}

public class QbPayment
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }

    public object? PopPaymentNumber { get; set; }

    public string UniqueId { get; set; }
    // public int? PopPaymentId { get; set; }
}

public class QbCheque
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }
    public object? PopChequeNumber { get; set; }
    public int? QbListId { get; set; }
    public string UniqueId { get; set; }
}

public class QbDeposit
{
    public object? PopDepositNumber { get; set; }
    public string? QbListId { get; set; }
    public string UniqueId { get; set; }
}