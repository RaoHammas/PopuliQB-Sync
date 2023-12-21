namespace PopuliQB_Tool.BusinessObjects;

public class QbInvoice
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }
    public int? PopInvoiceNumber { get; set; }
    public int? PopInvoiceId { get; set; }
}

public class QbMemo
{
    public string? QbCustomerListId { get; set; }
    public string? QbCustomerName { get; set; }
    public int? PopInvoiceNumber { get; set; }
    public int? PopInvoiceId { get; set; }

}

public class QbPayment
    {
        public string? QbCustomerListId { get; set; }
        public string? QbCustomerName { get; set; }
        public int? PopPaymentNumber { get; set; }
        // public int? PopPaymentId { get; set; }
    }