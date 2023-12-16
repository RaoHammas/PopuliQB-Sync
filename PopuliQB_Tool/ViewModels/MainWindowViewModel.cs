using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly QBConnectionCheckService _qbConnectionCheckService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QbCustomerService _customerService;
    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ObservableCollection<StatusMessage> _statisticsMessages = new();

    [ObservableProperty] private int _totalRecords = 0;
    [ObservableProperty] private int _progressCount = 0;
    [ObservableProperty] private string? _companyName = "";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel(
        MessageBoxService messageBoxService,
        QBConnectionCheckService qbConnectionCheckService,
        PopuliAccessService populiAccessService,
        QbCustomerService customerService
    )
    {
        _messageBoxService = messageBoxService;
        _qbConnectionCheckService = qbConnectionCheckService;
        _populiAccessService = populiAccessService;
        _customerService = customerService;

        _customerService.OnSyncStatusChanged += SyncStatusChanged;
        _customerService.OnSyncProgressChanged += SyncProgressChanged;
    }

    private void SyncProgressChanged(object? sender, ProgressArgs e)
    {
        ProgressCount += e.ProgressValue;
    }

    private void SyncStatusChanged (object? sender, StatusMessageArgs args)
    {
        SetSyncStatusMessage($"{args.StatusType}", args.StatusType);
    }

    [RelayCommand]
    private async Task GetAllASync()
    {
        try
        {
            await _customerService.GetPersonsAsync();
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand]
    private async Task ConnectToQb()
    {
        try
        {
            await Task.Run(() =>
            {
                if (_qbConnectionCheckService.OpenConnection())
                {
                    CompanyName = _qbConnectionCheckService.GetCompanyName();
                    SetSyncStatusMessage("Connected to QuickBooks.", StatusMessageType.Success);
                }
            });
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowError("Error", ex.Message);
            SetSyncStatusMessage("Failed to connect to QuickBooks.", StatusMessageType.Error);

            _qbConnectionCheckService.CloseConnection();
            SetSyncStatusMessage("QuickBooks isn't running.", StatusMessageType.Info);
            _logger.Error(ex);
        }
    }

    [RelayCommand]
    private async Task StartPopuliStudentsSync()
    {
        try
        {
            TotalRecords = 0;
            ProgressCount = 0;

            SetSyncStatusMessage($"Populi to QBD Sync. Version: {VersionHelper.Version}", StatusMessageType.Info);
            SetSyncStatusMessage("Starting Sync.", StatusMessageType.Info);

            var populiPersonsTask = Task.Run(() => _populiAccessService.GetAllPersonsAsync());
            var qbPersonsTask = Task.Run(() => _customerService.GetPersonsAsync());
            
            SetSyncStatusMessage("Fetching Persons from Populi.", StatusMessageType.Info);
            SetSyncStatusMessage("Fetching Persons from QB.", StatusMessageType.Info);
            await Task.WhenAll(populiPersonsTask, qbPersonsTask);

            var persons = populiPersonsTask.Result;
            if (persons.Any())
            {
                TotalRecords = persons.Count;
                SetSyncStatusMessage($"Fetched Persons from Populi : {persons.Count}", StatusMessageType.Success);

                var qbCustomers = qbPersonsTask.Result;
                if (qbCustomers.Any())
                {
                    SetSyncStatusMessage($"Fetched Persons from QB : {qbCustomers.Count}", StatusMessageType.Info);
                }

                var resp = await _customerService.AddCustomersAsync(persons.Take(20).ToList());
                if (resp)
                {
                    SetSyncStatusMessage($"Completed Successfully.", StatusMessageType.Success);
                    return;
                }

                SetSyncStatusMessage($"Failed.", StatusMessageType.Error);
            }
            else
            {
                SetSyncStatusMessage($"Fetched Persons : 0", StatusMessageType.Success);
            }
        }
        catch (Exception ex)
        {
            SetSyncStatusMessage("Failed to fetch Persons", StatusMessageType.Error);
            _logger.Error(ex);
        }
    }


    private void SetSyncStatusMessage(string message, StatusMessageType type)
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

    private void SetStatisticsMessage(string message, StatusMessageType type)
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
        _customerService.OnSyncStatusChanged -= SyncStatusChanged;
        _customerService.OnSyncProgressChanged -= SyncProgressChanged;

    }
}