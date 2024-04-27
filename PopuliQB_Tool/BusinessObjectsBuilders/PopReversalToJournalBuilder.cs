using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopReversalToJournalBuilder
{
    private readonly PopuliAccessService _populiAccessService;

    public PopReversalToJournalBuilder(PopuliAccessService populiAccessService)
    {
        _populiAccessService = populiAccessService;
    }

    public void BuildAddRequest(IMsgSetRequest requestMsgSet, string number, PopTransaction transaction,
        string studentName, string studentQbListId, DateTime transPostedOn)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendJournalEntryAddRq();
        request.TxnDate.SetValue(transPostedOn);
        //request.IsAdjustment.SetValue(true);

        if (transaction.LedgerEntries.Any())
        {
            foreach (var entry in transaction.LedgerEntries)
            {
                var orItem = request.ORJournalLineList.Append();

                var accListId = _populiAccessService.AllPopuliAccounts.First(x => x.Id == entry.AccountId).QbAccountListId;
               
                if (entry.Direction == "debit")
                {
                    orItem.JournalDebitLine.Amount.SetValue(Math.Abs(entry.Debit ?? 0));
                    orItem.JournalDebitLine.EntityRef.ListID.SetValue(studentQbListId);
                    orItem.JournalDebitLine.AccountRef.ListID.SetValue(accListId);
                    orItem.JournalDebitLine.Memo.SetValue($"Reversal of {number} for Student: {studentName}");
                }
                else
                {
                    orItem.JournalCreditLine.Amount.SetValue(Math.Abs(entry.Credit ?? 0));
                    orItem.JournalCreditLine.EntityRef.ListID.SetValue(studentQbListId);
                    orItem.JournalCreditLine.AccountRef.ListID.SetValue(accListId);
                    orItem.JournalCreditLine.Memo.SetValue($"Reversal of {number} for Student: {studentName}");
                }
            }
        }

        //request.IncludeRetElementList.Add("RefNumber");
    }

    public void BuildGetAllRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendJournalEntryQueryRq();
        request.IncludeRetElementList.Add("RefNumber");
    }
}