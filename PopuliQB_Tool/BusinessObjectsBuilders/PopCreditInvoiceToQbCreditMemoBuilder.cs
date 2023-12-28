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
        request.RefNumber.SetValue(memo.Number.ToString());

        request.ARAccountRef.ListID.SetValue(arListId);
        
        if (!string.IsNullOrEmpty(memo.Status) && memo.Status != "unpaid")
        {
            request.IsPending.SetValue(false);
        }

        if (Convert.ToDateTime(memo.DueOn) is var dueDate)
        {
            request.DueDate.SetValue(dueDate);
        }

        if (Convert.ToDateTime(memo.PostedOn) is var postedDate)
        {
            request.TxnDate.SetValue(postedDate);
        }

        request.IsToBePrinted.SetValue(false);
        request.IsToBeEmailed.SetValue(false);
        request.Memo.SetValue($"Trans#{memo.TransactionId}");

        if (memo.Items != null)
        {
            foreach (var item in memo.Items)
            {
                if (item.Amount is < 1)
                {
                    item.Amount = (double?)Math.Abs((decimal)item.Amount);
                }

                var invItem = request.ORCreditMemoLineAddList.Append();
                invItem.CreditMemoLineAdd.ItemRef.ListID.SetValue(item.ItemQbListId);
                invItem.CreditMemoLineAdd.Desc.SetValue(item.Description);
                invItem.CreditMemoLineAdd.Quantity.SetValue(1);
                invItem.CreditMemoLineAdd.ORRatePriceLevel.Rate.SetValue(item.Amount!.Value);
                invItem.CreditMemoLineAdd.Amount.SetValue(item.Amount!.Value);
                invItem.CreditMemoLineAdd.TaxAmount.SetValue(0);
            }
        }


        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCreditMemoQueryRq();
        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
    }
}