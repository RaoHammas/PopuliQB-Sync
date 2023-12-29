using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using PopuliQB_Tool.Services;

namespace PopuliQB_Tool.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly MessageBoxService _messageBoxService;
    private readonly QBCompanyService _qbCompanyService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QbCustomerService _qbCustomerService;
    private readonly QBInvoiceService _qbInvoiceService;
    [ObservableProperty] private QbAccountsService _qbAccountsService;
    private readonly QbItemService _qbItemService;

    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ICollectionView _filteredLogs;

    [ObservableProperty] private int _totalRecords = 0;
    [ObservableProperty] private int _progressCount = 0;
    [ObservableProperty] private string? _companyName = "";
    [ObservableProperty] private string? _title = "Populi to QuickBooks Sync";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel(
        MessageBoxService messageBoxService,
        QBCompanyService qbCompanyService,
        PopuliAccessService populiAccessService,
        QbCustomerService qbCustomerService,
        QBInvoiceService qbInvoiceService,
        QbAccountsService qbAccountsService,
        QbItemService qbItemService
    )
    {
        _messageBoxService = messageBoxService;
        _qbCompanyService = qbCompanyService;
        _populiAccessService = populiAccessService;
        _qbCustomerService = qbCustomerService;
        _qbInvoiceService = qbInvoiceService;
        _qbAccountsService = qbAccountsService;
        _qbItemService = qbItemService;

        _qbCustomerService.OnSyncStatusChanged += SyncStatusChanged;
        _qbCustomerService.OnSyncProgressChanged += SyncProgressChanged;

        _qbInvoiceService.OnSyncStatusChanged += SyncStatusChanged;
        _qbInvoiceService.OnSyncProgressChanged += SyncProgressChanged;

        _qbAccountsService.OnSyncStatusChanged += SyncStatusChanged;
        _qbAccountsService.OnSyncProgressChanged += SyncProgressChanged;

        _qbItemService.OnSyncStatusChanged += SyncStatusChanged;
        _qbItemService.OnSyncProgressChanged += SyncProgressChanged;

        FilteredLogs = CollectionViewSource.GetDefaultView(SyncStatusMessages);
        FilteredLogs.Filter = null;
    }

    private void SyncProgressChanged(object? sender, ProgressArgs e)
    {
        if (e.Total != null)
        {
            TotalRecords = 0;
            ProgressCount = e.ProgressValue;
            TotalRecords = e.Total.Value;
        }
        else
        {
            ProgressCount += e.ProgressValue;
        }
    }

    private void SyncStatusChanged(object? sender, StatusMessageArgs args)
    {
        SetSyncStatusMessage(args.StatusType, args.Message);
    }

    [RelayCommand]
    private Task Loaded()
    {
        return ConnectToQb();
    }

    private async Task ConnectToQb()
    {
        try
        {
            await Task.Run(() => { CompanyName = _qbCompanyService.GetCompanyName(); });
            Title += $" [ Connected to {CompanyName} ]";
            SetSyncStatusMessage(StatusMessageType.Success, $"Connected to QuickBooks {CompanyName}.");
        }
        catch (Exception ex)
        {
            SetSyncStatusMessage(StatusMessageType.Error, "Failed to connect to QuickBooks.");
            SetSyncStatusMessage(StatusMessageType.Error,
                "Make sure QuickBooks is running on your system or try reopening the tool.");
            _logger.Error(ex);
        }
    }

    [RelayCommand]
    private async Task StartPopuliStudentsSync()
    {
        try
        {
            SyncStatusMessages.Clear();
            TotalRecords = 0;
            ProgressCount = 0;

            SetSyncStatusMessage(StatusMessageType.Success, $"Populi to QBD Sync. Version: {VersionHelper.Version}");
            SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from QB.");

            var qbCustomers = await _qbCustomerService.GetAllExistingCustomersAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Persons from QB : {qbCustomers.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from Populi.");
            var page = 1;
            var persons = await _populiAccessService.GetAllPersonsAsync(page);
            TotalRecords = persons.Results ?? 0;
            SetSyncStatusMessage(StatusMessageType.Success, $"Total Persons found on Populi : {persons.Results}");

            if (persons.Data.Count == 0)
            {
                SetSyncStatusMessage(StatusMessageType.Warn, $"Fetched Persons : 0");
                return;
            }

            while (page <= persons.Pages)
            {
                if (page != 1)
                {
                    persons.Data.Clear();
                    SetSyncStatusMessage(StatusMessageType.Info,
                        $"Fetching next {persons.ResultsPerPage} from Populi.");

                    persons = await _populiAccessService.GetAllPersonsAsync(page);
                    SetSyncStatusMessage(StatusMessageType.Success,
                        $"Fetched {persons.Data.Count} Persons from Populi.");
                }

                page++;
                if (persons.Data.Count != 0)
                {
                    SetSyncStatusMessage(StatusMessageType.Info, $"Adding next {persons.Data.Count} to QB.");
                    var resp = await _qbCustomerService.AddCustomersAsync(persons.Data);
                }
                else
                {
                    SetSyncStatusMessage(StatusMessageType.Warn, $"Fetched Persons : 0");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            SetSyncStatusMessage(StatusMessageType.Error, $"Failed with error : {ex.Message}.");
            _logger.Error(ex);
        }
    }

    [RelayCommand]
    private async Task StartPopuliInvoicesSync()
    {
        try
        {
            SyncStatusMessages.Clear();

            TotalRecords = 0;
            ProgressCount = 0;

            SetSyncStatusMessage(StatusMessageType.Success, $"Populi to QBD Sync. Version: {VersionHelper.Version}");
            SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Invoices from QB.");
            await _qbInvoiceService.SyncAllExistingInvoicesAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Credit Memos from QB.");
            await _qbInvoiceService.SyncAllExistingMemosAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Payments from QB.");
            await _qbInvoiceService.SyncAllExistingPaymentsAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Invoices from Populi.");
            var page = 1;
            var popInvoices = await _populiAccessService.GetAllInvoicesAsync(page);
            TotalRecords = popInvoices.Results ?? 0;
            SetSyncStatusMessage(StatusMessageType.Success, $"Total Invoices found on Populi : {popInvoices.Results}");

            if (popInvoices.Data == null || popInvoices.Data.Count == 0)
            {
                SetSyncStatusMessage(StatusMessageType.Warn, $"Fetched Invoices : 0");
                return;
            }

            while (page <= popInvoices.Pages)
            {
                if (page != 1)
                {
                    popInvoices.Data.Clear();
                    SetSyncStatusMessage(StatusMessageType.Info,
                        $"Fetching next {popInvoices.ResultsPerPage} from Populi.");

                    popInvoices = await  _populiAccessService.GetAllInvoicesAsync(page);
                    SetSyncStatusMessage(StatusMessageType.Success,
                        $"Fetched {popInvoices.Data!.Count} Invoices from Populi.");
                }

                page++;
                if (popInvoices.Data.Count != 0)
                {
                    SetSyncStatusMessage(StatusMessageType.Info, $"Adding {popInvoices.Count} Invoices to QB.");
                    await _qbInvoiceService.AddInvoicesAsync(popInvoices.Data);
                }
                else
                {
                    SetSyncStatusMessage(StatusMessageType.Warn, $"Fetched Persons : 0");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            SetSyncStatusMessage(StatusMessageType.Error, $"Failed with error : {ex.Message}.");
            _logger.Error(ex);
        }
    }

    [RelayCommand]
    private async Task StartPopuliToQbAccountsSync()
    {
        SyncStatusMessages.Clear();
        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");
        await Task.Run(async () =>
        {
            SetSyncStatusMessage(StatusMessageType.Info, "Synching Accounts From QB.");
            await QbAccountsService.SyncAllExistingAccountsAsync();


            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Accounts From Populi.");
            await _populiAccessService.SyncAllAccountsAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched accounts: {_populiAccessService.AllPopuliAccounts.Count}.");

            foreach (var account in _populiAccessService.AllPopuliAccounts)
            {
                var qbAcc = QbAccountsService.AllExistingAccountsList
                    .FirstOrDefault(x =>
                        x.Number == account.AccountNumber
                        || x.Title.Trim().ToLower() == account.Name!.Trim().ToLower());
                if (qbAcc != null)
                {
                    account.QbAccountListId = qbAcc.ListId;
                }
                else
                {
                    SetSyncStatusMessage(StatusMessageType.Error, $"Populi Account [ {account.AccountNumber} | {account.Name} ] doesn't exist in QB.");
                }
            }

        });

        SetSyncStatusMessage(StatusMessageType.Success, $"Accounts List Sync Completed.");
    }

    [RelayCommand]
    private async Task StartExcelToQbItemsSync()
    {
        SyncStatusMessages.Clear();
        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

        await Task.Run(async () =>
        {


            SetSyncStatusMessage(StatusMessageType.Info, "Synching Items From QB.");
            await _qbItemService.SyncAllExistingItemsAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Synching Items From Excel.");
            const string path = "QB- Item List.xlsx";
            var sheetNames = MiniExcel.GetSheetNames(path);
            List<PopExcelItem> excelItems = new();
            foreach (var sheetName in sheetNames)
            {
                excelItems.AddRange(MiniExcel.Query<PopExcelItem>(path, sheetName: sheetName, excelType: ExcelType.XLSX)
                    .ToList());
            }

            //Refine data
            foreach (var item in excelItems)
            {
                if (item.Name.Length > 31) //31 is max length for Item name field in QB
                {
                    var name = item.Name.Substring(0, 31).Trim();
                    SetSyncStatusMessage(StatusMessageType.Warn, $"{item.Name} : trimmed to 31 chars. New name is: {name}");

                    item.Name = name.RemoveInvalidUnicodeCharacters();
                }
            }

            TotalRecords = excelItems.Count;
            ProgressCount = 0;

            await _qbItemService.AddExcelItemsAsync(excelItems);

        });

        SetSyncStatusMessage(StatusMessageType.Success, $"Items List Sync Completed.");
    }


    [RelayCommand]
    private void ClearLogs()
    {
        SyncStatusMessages.Clear();
    }

    [RelayCommand]
    private void SetSelectedLogType(StatusMessageType type)
    {
        if (type == StatusMessageType.All)
        {
            FilteredLogs.Filter = null; // Show all items
        }
        else
        {
            FilteredLogs.Filter = item => ((StatusMessage)item).MessageType == type;
        }
    }

    private void SetSyncStatusMessage(StatusMessageType type, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SyncStatusMessages.Insert(0, new StatusMessage
            {
                Message = message,
                MessageType = type
            });
        });
    }
    public void Dispose()
    {
        // TODO release managed resources here
        _qbCustomerService.OnSyncStatusChanged -= SyncStatusChanged;
        _qbCustomerService.OnSyncProgressChanged -= SyncProgressChanged;
    }
}