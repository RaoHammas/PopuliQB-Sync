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
}