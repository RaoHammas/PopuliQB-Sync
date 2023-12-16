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
        ProgressCount ++;
    }

    private void SyncStatusChanged(object? sender, StatusMessageArgs args)
    {
        SetSyncStatusMessage(args.StatusType, args.Message);
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
                    SetSyncStatusMessage(StatusMessageType.Success, "Connected to QuickBooks.");
                }
            });
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowError("Error", ex.Message);
            SetSyncStatusMessage(StatusMessageType.Error, "Failed to connect to QuickBooks.");

            _qbConnectionCheckService.CloseConnection();
            SetSyncStatusMessage(StatusMessageType.Info, "QuickBooks isn't running.");
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

            SetSyncStatusMessage(StatusMessageType.Info, $"Populi to QBD Sync. Version: {VersionHelper.Version}");
            SetSyncStatusMessage(StatusMessageType.Info, "Starting Sync.");

            var populiPersonsTask = Task.Run(() => _populiAccessService.GetAllPersonsAsync());
            var qbPersonsTask = Task.Run(() => _customerService.GetPersonsAsync());

            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from Populi.");
            SetSyncStatusMessage(StatusMessageType.Info, "Fetching Persons from QB.");
            await Task.WhenAll(populiPersonsTask, qbPersonsTask);

            var persons = populiPersonsTask.Result;
            if (persons.Any())
            {
                TotalRecords = 20;
                SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Persons from Populi : {persons.Count}");

                var qbCustomers = qbPersonsTask.Result;
                if (qbCustomers.Any())
                {
                    SetSyncStatusMessage(StatusMessageType.Info, $"Fetched Persons from QB : {qbCustomers.Count}");
                }

                var resp = await _customerService.AddCustomersAsync(persons.Take(20).ToList());
                if (resp)
                {
                    SetSyncStatusMessage(StatusMessageType.Success, $"Completed Successfully.");
                    return;
                }

                SetSyncStatusMessage(StatusMessageType.Error, $"Failed.");
            }
            else
            {
                SetSyncStatusMessage(StatusMessageType.Success, $"Fetched Persons : 0");
            }
        }
        catch (Exception ex)
        {
            SetSyncStatusMessage(StatusMessageType.Error, "Failed to fetch Persons");
            _logger.Error(ex);
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
        _customerService.OnSyncStatusChanged -= SyncStatusChanged;
        _customerService.OnSyncProgressChanged -= SyncProgressChanged;
    }
}