using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.Helpers;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopItemToQbItemBuilder
{
    public void BuildItemAddRequest(IMsgSetRequest requestMsgSet, PopItem item)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendItemServiceAddRq();
        var maxLength = Convert.ToInt32(request.Name.GetMaxLength());
        if (item.Name != null && item.Name.Length > maxLength)
        {
            request.Name.SetValue(item.Name[..maxLength]);
        }
        else
        {
            request.Name.SetValue(item.Name);
        }

        request.IsActive.SetValue(true);
        request.ORSalesPurchase.SalesOrPurchase.Desc.SetValue(item.Description ?? "");
        request.ORSalesPurchase.SalesOrPurchase.AccountRef.FullName.SetValue("Allowance for Tuition Rec (New)");
        request.ORSalesPurchase.SalesOrPurchase.ORPrice.Price.SetValue(item.Amount ?? 0);

        request.IncludeRetElementList.Add("ListID");
        request.IncludeRetElementList.Add("Name");
    }
    
    public void BuildExcelItemAddRequest(IMsgSetRequest requestMsgSet, PopExcelItem item)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendItemServiceAddRq();
        var maxLength = Convert.ToInt32(request.Name.GetMaxLength());
        if (item.Name.Length > maxLength)
        {
            var name = item.Name.Substring(0, maxLength);
            request.Name.SetValue(name);
            item.Name = name;
        }
        else
        {
            request.Name.SetValue(item.Name);
        }

        request.IsActive.SetValue(true);
        request.ORSalesPurchase.SalesOrPurchase.AccountRef.ListID.SetValue(item.QbAccListId);
        
        request.IncludeRetElementList.Add("ListID");
        request.IncludeRetElementList.Add("Name");
    }

    public void BuildGetAllQbItemsRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendItemServiceQueryRq();
        request.IncludeRetElementList.Add("ListID");
        request.IncludeRetElementList.Add("Name");
    }
}