using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopAccountsToQbAccountsBuilder
{
    public void BuildGetAllFromQbRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendAccountQueryRq();
    }
}