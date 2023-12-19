using PopuliQB_Tool.BusinessObjects;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPaymentToQbPaymentBuilder
{
    public void BuildInvoiceAddRequest(IMsgSetRequest requestMsgSet, PopInvoice invoice, string qbCustomerListId)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendInvoiceAddRq();
        request.CustomerRef.ListID.SetValue(qbCustomerListId);
        request.PONumber.SetValue(invoice.Id.ToString());
        request.RefNumber.SetValue(invoice.Number.ToString());


        if (!string.IsNullOrEmpty(invoice.Status) && invoice.Status != "unpaid")
        {
            request.IsPending.SetValue(false);
        }

        if (Convert.ToDateTime(invoice.ReportData?.InvoiceDueDate) is var dueDate)
        {
            request.DueDate.SetValue(dueDate);
        }

        /*if (Convert.ToDouble(invoice.ReportData?.AmountPaid) is var paid)
        {
            var setCredit11155 = invoiceAddRq.SetCreditList.Append();
            invoiceAddRq.LinkToTxnIDList.Add("200000-1011023419");
            setCredit11155.AppliedAmount.SetValue(paid);
            setCredit11155.CreditTxnID.SetValue("200000-1011023419");
        }*/

        request.IsToBePrinted.SetValue(false);
        request.IsToBeEmailed.SetValue(false);
        request.Memo.SetValue($"Trans#{invoice.TransactionId}");

        if (invoice.Items != null)
        {
            foreach (var item in invoice.Items)
            {
                var invItem = request.ORInvoiceLineAddList.Append();
                invItem.InvoiceLineAdd.ItemRef.FullName.SetValue(item.Name);
                invItem.InvoiceLineAdd.Desc.SetValue(item.Description);
                invItem.InvoiceLineAdd.Quantity.SetValue(1);
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