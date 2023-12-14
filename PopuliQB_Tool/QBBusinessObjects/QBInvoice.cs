using QBFC16Lib;

namespace PopuliQB_Tool.QBBusinessObjects;

class QBInvoice
{
    public string TxnID;
    public string EditSeq;
    public string CustomerListID;
    public double Subtotal;
    public List<QBItem> Items = new List<QBItem>();

    // Create Invoice (fills fields) from IInvoiceRet 
    public void CreateFromQBRet(IInvoiceRet qbInvoice)
    {
        TxnID = qbInvoice.TxnID.GetValue();
        // fill items
        for (int i = 0; i < qbInvoice.ORInvoiceLineRetList.Count; i++)
        {
            var qbInvRet = qbInvoice.ORInvoiceLineRetList.GetAt(i).InvoiceLineRet;
            var qbItem = new QBItem();
            qbItem.TxnLineID = qbInvRet.TxnLineID.GetValue();
            qbItem.ID = qbInvRet.ItemRef.ListID.GetValue();
            qbItem.Name = qbInvRet.ItemRef.FullName.GetValue();
            Items.Add(qbItem);
        }

        Subtotal = qbInvoice.Subtotal.GetValue();
    }
}