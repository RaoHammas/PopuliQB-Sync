namespace PopuliQB_Tool.QBBusinessObjects;

class CustomerIDs
{
    public string ListID;
    public string EditSeq;

    public void CopyFromCustomer(Customer customer)
    {
        ListID = customer.ListID;
        EditSeq = customer.EditSeq;
    }
}