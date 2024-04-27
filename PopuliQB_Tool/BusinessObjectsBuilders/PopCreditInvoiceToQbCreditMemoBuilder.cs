using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopCreditMemoToQbCreditMemoBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, PopCredit memo, string qbCustomerListId, string arListId)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCreditMemoAddRq();
        request.CustomerRef.ListID.SetValue(qbCustomerListId);
        request.PONumber.SetValue(memo.Id.ToString());
        request.RefNumber.SetValue(memo.Number!.ToString());
        request.ARAccountRef.ListID.SetValue(arListId);

        request.IsPending.SetValue(false);
        if (!string.IsNullOrEmpty(memo.Status) && memo.Status == "unpaid")
        {
            request.IsPending.SetValue(true);
        }

        if (memo.DueOn != null && Convert.ToDateTime(memo.DueOn) is var dueDate)
        {
            request.DueDate.SetValue(dueDate);
        }

        if (memo.PostedOn != null && Convert.ToDateTime(memo.PostedOn) is var postedDate)
        {
            request.TxnDate.SetValue(postedDate);
        }

        request.IsToBePrinted.SetValue(false);
        request.IsToBeEmailed.SetValue(false);
        
        if (memo.Items != null)
        {
            foreach (var item in memo.Items)
            {
                
                var invItem = request.ORCreditMemoLineAddList.Append();
                invItem.CreditMemoLineAdd.ItemRef.ListID.SetValue(item.ItemQbListId);
                invItem.CreditMemoLineAdd.Desc.SetValue(item.Description);
                invItem.CreditMemoLineAdd.Quantity.SetValue(1);
                item.Amount = Math.Abs(item.Amount ?? 0);
                invItem.CreditMemoLineAdd.ORRatePriceLevel.Rate.SetValue(item.Amount!.Value);
                invItem.CreditMemoLineAdd.Amount.SetValue(item.Amount!.Value);
                invItem.CreditMemoLineAdd.TaxAmount.SetValue(0);
            }
        }


        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
        request.IncludeRetElementList.Add("Memo");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCreditMemoQueryRq();
        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
        request.IncludeRetElementList.Add("Memo");

    }
}