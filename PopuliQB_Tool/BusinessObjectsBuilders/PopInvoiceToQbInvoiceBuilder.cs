using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopInvoiceToQbInvoiceBuilder
{
    public void BuildInvoiceAddRequest(IMsgSetRequest requestMsgSet, PopInvoice invoice, string qbCustomerListId, string qbArListId)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendInvoiceAddRq();
        request.CustomerRef.ListID.SetValue(qbCustomerListId); 
        request.PONumber.SetValue(invoice.Id.ToString());
        request.RefNumber.SetValue(invoice.Number.ToString());
        // request.ARAccountRef.ListID.SetValue(QbSettings.Instance.ARForInvoice.ListId);
        request.ARAccountRef.ListID.SetValue(qbArListId);
        
        if (!string.IsNullOrEmpty(invoice.Status) && invoice.Status != "unpaid")
        {
            request.IsPending.SetValue(false);
        }

        if (invoice.DueOn != null && Convert.ToDateTime(invoice.DueOn) is var dueDate)
        {
            request.DueDate.SetValue(dueDate);
        }

        if (invoice.PostedOn != null && Convert.ToDateTime(invoice.PostedOn) is var postedDate)
        {
            request.TxnDate.SetValue(postedDate);
        }
        
        request.Memo.SetValue($"Trans#{invoice.TransactionId}");
        
        if (invoice.Items != null)
        {
            foreach (var item in invoice.Items)
            {
                var invItem = request.ORInvoiceLineAddList.Append();

                invItem.InvoiceLineAdd.ItemRef.ListID.SetValue(item.ItemQbListId);
                invItem.InvoiceLineAdd.Desc.SetValue(item.Description);
                invItem.InvoiceLineAdd.Quantity.SetValue(1);

                item.Amount = Math.Abs(item.Amount ?? 0);
                invItem.InvoiceLineAdd.ORRatePriceLevel.Rate.SetValue(item.Amount!.Value);
                invItem.InvoiceLineAdd.Amount.SetValue(item.Amount!.Value);
                invItem.InvoiceLineAdd.IsTaxable.SetValue(false);
                invItem.InvoiceLineAdd.TaxAmount.SetValue(0);
            }
        }


        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
    }

    public void BuildGetAllInvoicesRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendInvoiceQueryRq();
        request.IncludeRetElementList.Add("CustomerRef");
        request.IncludeRetElementList.Add("RefNumber");
        request.IncludeRetElementList.Add("PONumber");
        request.IncludeRetElementList.Add("FullName");
    }
}