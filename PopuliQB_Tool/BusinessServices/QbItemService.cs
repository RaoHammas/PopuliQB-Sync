﻿using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbItemService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopItemToQbItemBuilder _builder;
    private readonly QbAccountsService _qbAccountsService;
    public List<QbItem> AllExistingItemsList { get; set; } = new();

    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }

    public QbItemService(
        PopItemToQbItemBuilder builder,
        QbAccountsService qbAccountsService
    )
    {
        _builder = builder;
        _qbAccountsService = qbAccountsService;
    }

    private string FormatItemName(string name)
    {
        //31 is max length for Item name field in QB
        name = name!.RemoveInvalidUnicodeCharacters();
        if (name.Length > 31) //31 is max length for Item name field in QB
        {
            name = name.Substring(0, 31).Trim();
        }

        return name;
    }

    public bool CheckIfItemExists(PopItem item)
    {

        item.Name = FormatItemName(item.Name!);
        var existingItem = AllExistingItemsList.FirstOrDefault(x => x.QbItemName!.ToLower().Trim() == item.Name.ToLower().Trim());
        if (existingItem == null)
        {
            return false;
        }

        item.ItemQbListId = existingItem!.QbListId;
        return true;
    }

    public async Task AddItemAsync(PopItem item)
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;
                _builder.BuildItemAddRequest(requestMsgSet, item);

                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadAddedItem(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                    _logger.Error(msg);
                    OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                }
                else
                {
                    OnSyncStatusChanged?.Invoke(this,
                        new StatusMessageArgs(StatusMessageType.Success, $"Added item {item.Name} to QB."));
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            throw;
        }
        finally
        {
            if (isSessionOpen)
            {
                sessionManager.EndSession();
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (isConnected)
            {
                sessionManager.CloseConnection();
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    public async Task AddExcelItemsAsync(List<PopExcelItem> excelItems)
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(async () =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                foreach (var excelItem in excelItems)
                {
                    //await Task.Delay(1000);
                    
                    excelItem.Name = FormatItemName(excelItem.Name);
                    var qbItem = AllExistingItemsList.FirstOrDefault(x =>
                        (x.QbItemName == null ? "" : x.QbItemName.ToLower().Trim()) == excelItem.Name.ToLower().Trim());

                    if (qbItem == null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error, $"{excelItem.Name} does not exist in QB."));
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Info, $"Adding {excelItem.Name} to QB."));

                        var acc = _qbAccountsService.AllExistingAccountsList.FirstOrDefault(x =>
                            x.FullName == excelItem.Account.Trim()
                            || x.Number == excelItem.AccNumberOnly.Trim()
                            || x.Title.Contains(excelItem.AccTitleOnly.Trim()));

                        if (acc == null)
                        {
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Error,
                                    $"{excelItem.Account} does not exist in QB."));
                            OnSyncStatusChanged?.Invoke(this,
                                new StatusMessageArgs(StatusMessageType.Error,
                                    $"Failed to add {excelItem.Name} to QB."));
                        }
                        else
                        {
                            excelItem.QbAccListId = acc.ListId;
                            _builder.BuildExcelItemAddRequest(requestMsgSet, excelItem);

                            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                            if (!ReadAddedItem(responseMsgSet))
                            {
                                var xmResp = responseMsgSet.ToXMLString();
                                var msg = PqExtensions.GetXmlNodeValue(xmResp);
                                _logger.Error(msg);

                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                            }
                            else
                            {
                                OnSyncStatusChanged?.Invoke(this,
                                    new StatusMessageArgs(StatusMessageType.Success,
                                        $"Added item {excelItem.Name} to QB."));
                            }

                        }
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn, $"{excelItem.Name} already exists in QB."));
                    }

                    OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, $"Completed."));
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
        }
        finally
        {
            if (isSessionOpen)
            {
                sessionManager.EndSession();
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (isConnected)
            {
                sessionManager.CloseConnection();
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private bool ReadAddedItem(IMsgSetResponse? responseMsgSet)
    {
        var responseList = responseMsgSet?.ResponseList;
        if (responseList == null)
        {
            return false;
        }

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemServiceAddRs)
                    {
                        var ret = (IItemServiceRet)response.Detail;
                        if (ret != null)
                        {
                            ReadPropertiesItem(ret);
                            return true;
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    #region GET ALL ITEMS

    public async Task SyncAllExistingItemsAsync()
    {
        var sessionManager = new QBSessionManager();
        var isConnected = false;
        var isSessionOpen = false;

        try
        {
            AllExistingItemsList.Clear();
            sessionManager.OpenConnection2(QBCompanyService.AppId, QBCompanyService.AppName, ENConnectionType.ctLocalQBD);
            isConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
            isSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _builder.BuildGetAllQbItemsRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                if (!ReadFetchedItem(responseMsgSet))
                {
                    var xmResp = responseMsgSet.ToXMLString();
                    var msg = PqExtensions.GetXmlNodeValue(xmResp);
                    _logger.Error(msg);
                    OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{msg}"));
                }
            });

            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Success,
                    $"Completed: Found items in QB {AllExistingItemsList.Count}"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
        }
        finally
        {
            if (isSessionOpen)
            {
                sessionManager.EndSession();
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (isConnected)
            {
                sessionManager.CloseConnection();
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private bool ReadFetchedItem(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return false;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return false;

        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            if (response.StatusCode >= 0)
            {
                if (response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemServiceQueryRs)
                    {
                        var retList = (IItemServiceRetList)response.Detail;
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(0, retList.Count));
                        for (var x = 0; x < retList.Count; x++)
                        {
                            ReadPropertiesItem(retList.GetAt(x));
                            OnSyncProgressChanged?.Invoke(this, new ProgressArgs(1));
                        }

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private QbItem? ReadPropertiesItem(IItemServiceRet? ret)
    {
        try
        {
            if (ret == null) return null;

            var item = new QbItem()
            {
                QbListId = ret.ListID.GetValue(),
                QbItemName = ret.Name.GetValue(),
            };

            AllExistingItemsList.Add(item);
            OnSyncStatusChanged?.Invoke(this,
                new StatusMessageArgs(StatusMessageType.Info, $"Found: {item.QbItemName}"));
            return item;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, $"{ex.Message}"));
        }

        return null;
    }

    #endregion
}