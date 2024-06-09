using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;

namespace PopuliQB_Tool.Services;

public class CommonOperationsService
{
    private readonly PopuliAccessService _populiAccessService;

    public CommonOperationsService(PopuliAccessService populiAccessService)
    {
        _populiAccessService = populiAccessService;
    }

    public int GetPopuliAccountReceivableId(List<PopLedgerEntry> entries)
    {
        
        foreach (var entry in entries)
        {
            if (entry.Credit > 0)
            {
                var acc = _populiAccessService.AllPopuliAccounts.FirstOrDefault(x => x.Id == entry.AccountId);
                if (acc != null && acc.Type?.ToLower() == "Asset".ToLower())
                {
                    return entry.AccountId ?? 0;
                }
            }
        }

        return entries.First(x => x.Direction == "debit").AccountId ?? 0;
    }
}