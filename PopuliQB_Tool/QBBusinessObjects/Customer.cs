namespace PopuliQB_Tool.QBBusinessObjects;

class Customer
{
    public string ListID;
    public string EditSeq;
    public string Name;
    public string FullName;
    public string CompanyName;
    public string Email;
    public string Phone;
    public Address BillAddr = new Address();
    public string MainPhone;
    public string WorkPhone;
    public string HomePhone;
    public string Mobile;
    public string AltPhone;
    public string AltMobile;
}