using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPaymentToQbPaymentBuilder
{
    public void BuildAddRequest(IMsgSetRequest requestMsgSet, PopPayment payment, string qbCustomerListId,
        string arListId, string adListId)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendReceivePaymentAddRq();
        request.CustomerRef.ListID.SetValue(qbCustomerListId);
        request.RefNumber.SetValue(payment.Number.ToString());
        
        request.ARAccountRef.ListID.SetValue(arListId);
        request.DepositToAccountRef.ListID.SetValue(adListId);
        
        request.TotalAmount.SetValue(payment.Amount ?? 0);
        request.ORApplyPayment.IsAutoApply.SetValue(true);
        
        /*if (Convert.ToDateTime(payment.DueOn) is var dueDate)
        {
            request.DueDate.SetValue(dueDate);
        }

        if (Convert.ToDateTime(payment.PostedOn) is var postedDate)
        {
            request.TxnDate.SetValue(postedDate);
        }*/

        request.Memo.SetValue(
            $"PaidType# {payment.PaidByType} Trans#{payment.TransactionId} Receipt#{payment.ReceiptNumber} Id#{payment.Id.ToString()}");


        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("FullName");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendReceivePaymentQueryRq();
        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("FullName");
    }
}