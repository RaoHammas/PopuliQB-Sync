namespace PopuliQB_Tool.BusinessObjects;

public sealed class QbSettings
{
    private static readonly Lazy<QbSettings> Lazy = new(() => new QbSettings());
    public static QbSettings Instance => Lazy.Value;

    private QbSettings()
    {
    }

    public QbAccount ARForInvoice { get; set; } = new QbAccount();
    public QbAccount ARForCreditMemos { get; set; } = new QbAccount();
    public QbAccount ARForPayments { get; set; } = new QbAccount();
    public QbAccount ADForPayments { get; set; } = new QbAccount();

    public DateTime PostedFrom { get; set; } = DateTime.UtcNow;
    public DateTime PostedTo { get; set; } = DateTime.UtcNow;
    public bool ApplyPostedDateFilter { get; set; }

    public DateTime AddedFrom { get; set; } = DateTime.UtcNow;
    public DateTime AddedTo { get; set; } = DateTime.UtcNow;
    public bool ApplyAddedDateFilter { get; set; }

    public string InvoiceNumFrom { get; set; } = "";
    public string InvoiceNumTo { get; set; } = "";
    public bool ApplyInvoiceNumFilter { get; set; }

    public PopPerson Student { get; set; }
    public bool ApplyStudentFilter { get; set; }
}