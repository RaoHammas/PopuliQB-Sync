namespace PopuliQB_Tool.BusinessObjects;

public class Invoice
{
    public int id { get; set; }
    public int number;
    public double amount;
    public DateTime posted_on;
    public DateTime due_on;
    public InvReportData report_data;
    public List<Item> items = new List<Item>();
}