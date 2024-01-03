using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniExcelLibs;
using NLog;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Models;
using PopuliQB_Tool.Services;

namespace PopuliQB_Tool.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly MessageBoxService _messageBoxService;
    private readonly QBCompanyService _qbCompanyService;
    [ObservableProperty] private bool _isAccountsListSynced = false;
    [ObservableProperty] private bool _isItemsListSynced = false;
    [ObservableProperty] private bool _isStudentsListListSynced = false;

    private readonly QBInvoiceService _qbInvoiceService;
    private readonly QbItemService _qbItemService;

    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ICollectionView _filteredLogs;
    [ObservableProperty] private PopuliAccessService _populiAccessService;
    [ObservableProperty] private QbCustomerService _qbCustomerService;
    [ObservableProperty] private QbAccountsService _qbAccountsService;
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
        PopuliAccessService = populiAccessService;
        QbCustomerService = qbCustomerService;
        _qbInvoiceService = qbInvoiceService;
        QbAccountsService = qbAccountsService;
        _qbItemService = qbItemService;

        QbCustomerService.OnSyncStatusChanged += SyncStatusChanged;
        QbCustomerService.OnSyncProgressChanged += SyncProgressChanged;

        _qbInvoiceService.OnSyncStatusChanged += SyncStatusChanged;
        _qbInvoiceService.OnSyncProgressChanged += SyncProgressChanged;

        QbAccountsService.OnSyncStatusChanged += SyncStatusChanged;
        QbAccountsService.OnSyncProgressChanged += SyncProgressChanged;

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
        SyncStatusMessages.Clear();
        TotalRecords = 0;
        ProgressCount = 0;

        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

        try
        {
            PopuliAccessService.AllPopuliPersons.Clear();

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from QB.");
            await QbCustomerService.SyncAllExistingCustomersAsync();
            SetSyncStatusMessage(StatusMessageType.Success,
                $"Fetched Persons from QB: {QbCustomerService.AllExistingCustomersList.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from Populi.");
            var page = 1;
            var persons = await PopuliAccessService.GetAllPersonsAsync(page);
            TotalRecords = persons.Results ?? 0;
            SetSyncStatusMessage(StatusMessageType.Success, $"Total Persons found on Populi: {persons.Results}");

            if (persons.Data!.Count == 0)
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

                    persons = await PopuliAccessService.GetAllPersonsAsync(page);
                    SetSyncStatusMessage(StatusMessageType.Success,
                        $"Fetched {persons.Data!.Count} Persons from Populi.");
                }

                page++;
                if (persons.Data.Count != 0)
                {
                    SetSyncStatusMessage(StatusMessageType.Info, $"Adding next {persons.Data.Count} to QB.");
                    var resp = await QbCustomerService.AddCustomersAsync(persons.Data);
                }
                else
                {
                    SetSyncStatusMessage(StatusMessageType.Warn, $"Fetched Persons : 0");
                    return;
                }
            }

            IsStudentsListListSynced = true;
            SetSyncStatusMessage(StatusMessageType.Success, $"Students List Sync Completed.");
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
        if (string.IsNullOrEmpty(QbSettings.Instance.ADForPayments.FullName))
        {
            _messageBoxService.ShowError("Error", "Select a Deposit account for Payments.");
            return;
        }

        SyncStatusMessages.Clear();
        TotalRecords = 0;
        ProgressCount = 0;

        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");
        try
        {
            PopuliAccessService.AllPopuliInvoices.Clear();

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Invoices from QB.");
            await _qbInvoiceService.SyncAllExistingInvoicesAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Credit Memos from QB.");
            await _qbInvoiceService.SyncAllExistingMemosAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Syncing Payments from QB.");
            await _qbInvoiceService.SyncAllExistingPaymentsAsync();

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Invoices from Populi.");
            var page = 1;
            var popInvoices = await PopuliAccessService.GetAllInvoicesAsync(page);
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

                    popInvoices = await PopuliAccessService.GetAllInvoicesAsync(page);
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
        TotalRecords = 0;
        ProgressCount = 0;

        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");
        await Task.Run(async () =>
        {
            SetSyncStatusMessage(StatusMessageType.Info, "Synching Accounts From QB.");
            await QbAccountsService.SyncAllExistingAccountsAsync();


            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Accounts From Populi.");
            await PopuliAccessService.SyncAllAccountsAsync();
            SetSyncStatusMessage(StatusMessageType.Success,
                $"Fetched accounts: {PopuliAccessService.AllPopuliAccounts.Count}.");

            foreach (var account in PopuliAccessService.AllPopuliAccounts)
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
                    SetSyncStatusMessage(StatusMessageType.Error,
                        $"Populi Acc No: {account.AccountNumber}  Name: {account.Name} doesn't exist in QB.");
                }
            }
        });

        if (QbAccountsService.AllExistingAccountsList.Any())
        {
            var dpAcc = QbAccountsService.AllExistingAccountsList.FirstOrDefault(x =>
                x.Title == "Truist Main Deposit (9155)");

            if (dpAcc != null)
            {
                QbSettings.Instance.ADForPayments = dpAcc;
            }
        }

        IsAccountsListSynced = true;
        SetSyncStatusMessage(StatusMessageType.Success, $"Accounts List Sync Completed.");
    }

    [RelayCommand]
    private async Task StartExcelToQbItemsSync()
    {
        SyncStatusMessages.Clear();
        TotalRecords = 0;
        ProgressCount = 0;

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

            TotalRecords = excelItems.Count;
            ProgressCount = 0;

            await _qbItemService.AddExcelItemsAsync(excelItems);
        });

        IsItemsListSynced = true;
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
        QbCustomerService.OnSyncStatusChanged -= SyncStatusChanged;
        QbCustomerService.OnSyncProgressChanged -= SyncProgressChanged;
    }
}