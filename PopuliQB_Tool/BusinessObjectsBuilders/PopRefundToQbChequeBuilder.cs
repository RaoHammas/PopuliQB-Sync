using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopRefundToQbChequeBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, PopCredit refund, string qbCustomerListId,
        string bankAccListId, DateTime transPostedOn)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCheckAddRq();
       
        request.PayeeEntityRef.ListID.SetValue(qbCustomerListId);
        request.RefNumber.SetValue(refund.Number.ToString());
        request.TxnDate.SetValue(transPostedOn);
        
        request.AccountRef.ListID.SetValue(bankAccListId);

        request.Memo.SetValue($"Ref#{refund.Number}");

        if (refund.Items != null)
        {
            foreach (var item in refund.Items)
            {
                var orItem = request.ORItemLineAddList.Append();
                orItem.ItemLineAdd.ItemRef.ListID.SetValue(item.ItemQbListId);
                orItem.ItemLineAdd.Desc.SetValue(item.Description ?? "");
                orItem.ItemLineAdd.Quantity.SetValue(1);
                orItem.ItemLineAdd.Cost.SetValue(item.Amount ?? 0);
                orItem.ItemLineAdd.Amount.SetValue(item.Amount ?? 0);
                orItem.ItemLineAdd.CustomerRef.ListID.SetValue(qbCustomerListId);
                orItem.ItemLineAdd.BillableStatus.SetValue(ENBillableStatus.bsHasBeenBilled);
            }
        }


        request.IncludeRetElementList.Add("PayeeEntityRef");
        request.IncludeRetElementList.Add("Memo");
        request.IncludeRetElementList.Add("FullName");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCheckQueryRq();
        request.IncludeRetElementList.Add("PayeeEntityRef");
        request.IncludeRetElementList.Add("Memo");
        request.IncludeRetElementList.Add("FullName");
    }
}