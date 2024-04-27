using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopRefundToQbChequeBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, PopCredit refund, string qbCustomerListId,
        string bankAccListId, string recAccListId, DateTime transPostedOn)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCheckAddRq();

        request.PayeeEntityRef.ListID.SetValue(qbCustomerListId);
        request.RefNumber.SetValue(refund.Number!.ToString());
        request.TxnDate.SetValue(transPostedOn);

        request.AccountRef.ListID.SetValue(bankAccListId);

        if (refund.Items != null)
        {
            foreach (var item in refund.Items)
            {
                var orItem = request.ExpenseLineAddList.Append();
                orItem.AccountRef.ListID.SetValue(recAccListId);
                item.Amount = Math.Abs(item.Amount ?? 0);
                orItem.Amount.SetValue(item.Amount ?? 0);
                orItem.Memo.SetValue(item.Name);
                // orItem.BillableStatus.SetValue(ENBillableStatus.bsNotBillable);
                orItem.CustomerRef.ListID.SetValue(qbCustomerListId);
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