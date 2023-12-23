using System.Collections.ObjectModel;
using System.Windows;
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

    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ObservableCollection<StatusMessage> _statisticsMessages = new();

    [ObservableProperty] private int _totalRecords = 0;
    [ObservableProperty] private int _progressCount = 0;
    [ObservableProperty] private string? _companyName = "";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel(
        MessageBoxService messageBoxService,
        QBCompanyService qbCompanyService,
        PopuliAccessService populiAccessService,
        QbCustomerService qbCustomerService,
        QBInvoiceService qbInvoiceService,
        QbAccountsService qbAccountsService
    )
    {
        _messageBoxService = messageBoxService;
        _qbCompanyService = qbCompanyService;
        _populiAccessService = populiAccessService;
        _qbCustomerService = qbCustomerService;
        _qbInvoiceService = qbInvoiceService;
        _qbAccountsService = qbAccountsService;

        _qbCustomerService.OnSyncStatusChanged += SyncStatusChanged;
        _qbCustomerService.OnSyncProgressChanged += SyncProgressChanged;

        _qbInvoiceService.OnSyncStatusChanged += SyncStatusChanged;
        _qbInvoiceService.OnSyncProgressChanged += SyncProgressChanged;

        _qbAccountsService.OnSyncStatusChanged += SyncStatusChanged;
        _qbAccountsService.OnSyncProgressChanged += SyncProgressChanged;
    }

    private void SyncProgressChanged(object? sender, ProgressArgs e)
    {
        if (e.Total != null)
        {
            TotalRecords = e.Total.Value;
        }

        ProgressCount++;
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

    private Task<PopResponse<PopPerson>> GetNextBatchOfPersonsFromPopuli(int page)
    {
        return _populiAccessService.GetAllPersonsAsync(page);
    }

    private Task<PopResponse<PopInvoice>> GetNextBatchOfInvoicesAndCreditsAndPaymentsFromPopuli(int page)
    {
        return _populiAccessService.GetAllInvoicesAsync(page);
    }

    [RelayCommand]
    private async Task StartPopuliStudentsSync()
    {
        try
        {
            TotalRecords = 0;
            ProgressCount = 0;

            SetSyncStatusMessage(StatusMessageType.Success, $"Populi to QBD Sync. Version: {VersionHelper.Version}");
            SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from QB.");

            var qbCustomers = await _qbCustomerService.GetAllExistingCustomersAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Persons from QB : {qbCustomers.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from Populi.");
            var page = 1;
            var persons = await GetNextBatchOfPersonsFromPopuli(page);
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

                    persons = await GetNextBatchOfPersonsFromPopuli(page);
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
            TotalRecords = 0;
            ProgressCount = 0;

            SetSyncStatusMessage(StatusMessageType.Success, $"Populi to QBD Sync. Version: {VersionHelper.Version}");
            SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Invoices from QB.");
            var qbInvoices = await _qbInvoiceService.GetAllExistingInvoicesAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Invoices from QB : {qbInvoices.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Credit Memos from QB.");
            var qbMemos = await _qbInvoiceService.GetAllExistingMemosAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Credit Memos from QB : {qbMemos.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Payments from QB.");
            var qbPayments = await _qbInvoiceService.GetAllExistingPaymentsAsync();
            SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Payments from QB : {qbPayments.Count}");

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Invoices from Populi.");
            var page = 1;
            var popInvoices = await GetNextBatchOfInvoicesAndCreditsAndPaymentsFromPopuli(page);
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

                    popInvoices = await GetNextBatchOfInvoicesAndCreditsAndPaymentsFromPopuli(page);
                    SetSyncStatusMessage(StatusMessageType.Success,
                        $"Fetched {popInvoices.Data.Count} Invoices from Populi.");
                }

                page++;
                if (popInvoices.Data.Count != 0)
                {
                    SetSyncStatusMessage(StatusMessageType.Info, $"Adding {popInvoices.Count} Invoices to QB.");
                    var resp = await _qbInvoiceService.AddInvoicesAsync(popInvoices.Data);
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
    private async Task StartExcelToQbItemsSync()
    {
        SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");
        SetSyncStatusMessage(StatusMessageType.Info, "Starting Reading Accounts From QB.");

        var accounts = await _qbAccountsService.GetAllExistingAccountsAsync();
        SetSyncStatusMessage(StatusMessageType.Success, $"Found accounts in QB {accounts.Count}");

        SetSyncStatusMessage(StatusMessageType.Info, "Starting Reading Items From Excel.");
        await Task.Run(async () =>
        {
            var path = "QB- Item List.xlsx";
            var sheetNames = MiniExcel.GetSheetNames(path);
            foreach (var sheetName in sheetNames)
            {
                var items = MiniExcel.Query<PopExcelItem>(path, sheetName: sheetName, excelType:ExcelType.XLSX).ToList();
                
                TotalRecords = items.Count;
                ProgressCount = 0;
                SetSyncStatusMessage(StatusMessageType.Info, $"Starting Reading Items From {sheetName}.");

                foreach (var item in items)
                {
                    var acc = accounts.FirstOrDefault(x => x.FullName == item.Account.Trim()
                                                           || x.Number == item.AccNumberOnly.Trim()
                                                           || x.Title.Contains(item.AccTitleOnly.Trim()));
                    if (acc == null)
                    {
                        SetSyncStatusMessage(StatusMessageType.Error, $"{item.Account} does not exist in QB.");
                        ProgressCount++;
                        continue;
                    }

                    SetSyncStatusMessage(StatusMessageType.Info, $"{item.Account}.");
                    item.QbAccListId = acc.ListId;
                    ProgressCount++;
                    await Task.Delay(50);
                }

                SetSyncStatusMessage(StatusMessageType.Info, $"Done Reading Items From {sheetName} - {items.Count}.");
            }

        });

        SetSyncStatusMessage(StatusMessageType.Success, $"Items List Sync Completed.");
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

    private void SetStatisticsMessage(StatusMessageType type, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatisticsMessages.Insert(0, new StatusMessage
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