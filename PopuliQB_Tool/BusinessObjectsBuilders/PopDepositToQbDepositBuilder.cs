using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopDepositToQbDepositBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, PopCredit memo, string qbCustomerListId,
        string fromAccListId, string depositAccListId, DateTime transDate)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendDepositAddRq();
        request.TxnDate.SetValue(transDate);
        request.DepositToAccountRef.ListID.SetValue(depositAccListId);

        if (memo.Items != null)
        {
            foreach (var item in memo.Items)
            {
                var invItem = request.DepositLineAddList.Append();
                invItem.ORDepositLineAdd.DepositInfo.EntityRef.ListID.SetValue(qbCustomerListId);
                invItem.ORDepositLineAdd.DepositInfo.AccountRef.ListID.SetValue(fromAccListId);
                item.Amount = item.Amount ?? 0;
                invItem.ORDepositLineAdd.DepositInfo.Amount.SetValue(item.Amount ?? 0);
            }
        }

        request.IncludeRetElementList.Add("Memo");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendDepositQueryRq();
       
        request.IncludeRetElementList.Add("Memo");
    }
}