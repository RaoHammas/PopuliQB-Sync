using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPaymentToQbPaymentBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, string key, PopPayment payment, string qbCustomerListId,
        string arListId, string adListId, DateTime transPostedOn)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendReceivePaymentAddRq();
        request.CustomerRef.ListID.SetValue(qbCustomerListId);
        request.RefNumber.SetValue(payment.Number!.ToString());
        request.TxnDate.SetValue(transPostedOn);
        request.ARAccountRef.ListID.SetValue(arListId);
        request.DepositToAccountRef.ListID.SetValue(adListId);
        
        request.TotalAmount.SetValue(payment.Amount ?? 0);
        request.ORApplyPayment.IsAutoApply.SetValue(true);

        request.Memo.SetValue(key);

        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("FullName");
        request.IncludeRetElementList.Add("Memo");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendReceivePaymentQueryRq();
        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("FullName");
        request.IncludeRetElementList.Add("Memo");

    }
}