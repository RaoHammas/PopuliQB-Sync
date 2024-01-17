using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.Helpers;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class CustomFieldBuilderQuick
{
    public void AddCustomField(QBSessionManager sessionManager, ENAssignToObject dataType, string dataListId,
        string fieldName, string fieldValue)
    {
        var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
        requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

        var dataExtDefRq = requestMsgSet.AppendDataExtDefAddRq();

        dataExtDefRq.DataExtName.SetValue(fieldName);
        dataExtDefRq.DataExtType.SetValue(ENDataExtType.detSTR255TYPE);
        dataExtDefRq.AssignToObjectList.Add(dataType);
        dataExtDefRq.OwnerID.SetValue("0");
        var responseMsgSetDef = sessionManager.DoRequests(requestMsgSet);
        var xmRespDef = responseMsgSetDef.ToXMLString();
        var msgDef = PqExtensions.GetXmlNodeValue(xmRespDef);

        requestMsgSet.ClearRequests();
        var dataExtAddRq = requestMsgSet.AppendDataExtAddRq();
        dataExtAddRq.OwnerID.SetValue("0");
        dataExtAddRq.DataExtName.SetValue(fieldName);
        dataExtAddRq.ORListTxnWithMacro.ListDataExt.ListDataExtType.SetValue(ENListDataExtType.ldetCustomer);
        
        dataExtAddRq.ORListTxnWithMacro.ListDataExt.ListObjRef.ListID.SetValue(dataListId);

        dataExtAddRq.DataExtValue.SetValue(fieldValue);
        var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
        var xmResp = responseMsgSet.ToXMLString();
        var msg = PqExtensions.GetXmlNodeValue(xmResp);
    }

    public string GetFieldValue(IDataExtRetList dataExtRetList, string fieldName)
    {
        for (int x = 0; x < dataExtRetList.Count; x++)
        {
            var dataExt = dataExtRetList.GetAt(x);
            var customFieldName = dataExt.DataExtName.GetValue();
            if (customFieldName == fieldName)
            {
                return dataExt.DataExtValue.GetValue();
            }
        }

        return "";
    }
}