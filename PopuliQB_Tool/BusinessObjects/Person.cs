namespace PopuliQB_Tool.BusinessObjects;

public class Person
{
    public int id;
    public string first_name { get; set; }
    public string last_name { get; set; }
    public DateTime added_at { get; set; }
    public List<PhoneNumber> phone_numbers { get; set; }
    public List<EmailAddr> email_addresses { get; set; }
    public ReportData report_data { get; set; }

    public Student student { get; set; }
}