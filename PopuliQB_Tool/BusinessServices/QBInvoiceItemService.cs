using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.Helpers;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QBInvoiceItemService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopInvoiceItemToQbInvoiceItemBuilder _builder;

    public List<QbInvoiceServiceItem> AllExistingInvoiceServiceItemsList { get; set; } = new();

    public QBInvoiceItemService(
        PopInvoiceItemToQbInvoiceItemBuilder builder)
    {
        _builder = builder;
    }

    public async Task<bool> AddInvoiceServiceItemAsync(PopInvoiceItem item, QBSessionManager sessionManager, IMsgSetRequest requestMsgSet)
    {
        try
        {
            await Task.Run(() =>
            {
                _builder.BuildInvoiceItemAddRequest(requestMsgSet, item);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadAddedInvoiceServiceItem(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PQExtensions.GetXmlNodeValue(xmResp);
                    _logger.Error(msg);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            throw;
        }
    }

    private bool ReadAddedInvoiceServiceItem(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null)
        {
            return false;
        }

        var responseList = responseMsgSet.ResponseList;
        if (responseList == null)
        {
            return false;
        }

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemServiceAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var customerRet = (IItemServiceRet)response.Detail;
                        if (customerRet != null)
                        {
                            ReadProperties(customerRet);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    #region GET ALL INVOICES

    public async Task<List<QbInvoiceServiceItem>> GetAllExistingInvoiceServiceItemsAsync(
        QBSessionManager sessionManager, IMsgSetRequest requestMsgSet)
    {
        try
        {
            AllExistingInvoiceServiceItemsList.Clear();

            await Task.Run(() =>
            {
                _builder.BuildGetAllInvoiceItemsRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                var xmResp = responseMsgSet.ToXMLString();
                ReadFetchedInvoiceServiceItem(responseMsgSet);
            });

            return AllExistingInvoiceServiceItemsList;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return AllExistingInvoiceServiceItemsList;
        }
    }

    private void ReadFetchedInvoiceServiceItem(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return;

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemServiceQueryRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var retList = (IItemServiceRetList)response.Detail;

                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadProperties(retList.GetAt(x));
                        }
                    }
                }
            }
        }
    }

    private QbInvoiceServiceItem? ReadProperties(IItemServiceRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var item = new QbInvoiceServiceItem()
            {
                QbListId = ret.ListID.GetValue(),
                QbItemName = ret.Name.GetValue(),
            };

            AllExistingInvoiceServiceItemsList.Add(item);
            return item;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;
    }

    #endregion
}