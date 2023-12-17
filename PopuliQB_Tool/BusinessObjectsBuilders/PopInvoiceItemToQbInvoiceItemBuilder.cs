using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopInvoiceItemToQbInvoiceItemBuilder
{
    public PopInvoiceItemToQbInvoiceItemBuilder()
    {
    }

    public void BuildInvoiceItemAddRequest(IMsgSetRequest requestMsgSet, PopInvoiceItem invoiceItem)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendItemServiceAddRq();
        var maxLength = Convert.ToInt32(request.Name.GetMaxLength());
        if (invoiceItem.Name != null && invoiceItem.Name.Length > maxLength)
        {
            request.Name.SetValue(invoiceItem.Name[..maxLength]);
        }
        else
        {
            request.Name.SetValue(invoiceItem.Name);
        }

        request.IsActive.SetValue(true);
        request.ORSalesPurchase.SalesOrPurchase.Desc.SetValue(invoiceItem.Description);
        request.ORSalesPurchase.SalesOrPurchase.AccountRef.FullName.SetValue("Allowance for Tuition Rec (New)");
        request.ORSalesPurchase.SalesOrPurchase.ORPrice.Price.SetValue(invoiceItem.Amount ?? 0);

        request.IncludeRetElementList.Add("ListID");
        request.IncludeRetElementList.Add("Name");
    }

    public void BuildGetAllInvoiceItemsRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendItemServiceQueryRq();
        request.IncludeRetElementList.Add("ListID");
        request.IncludeRetElementList.Add("Name");
    }
}