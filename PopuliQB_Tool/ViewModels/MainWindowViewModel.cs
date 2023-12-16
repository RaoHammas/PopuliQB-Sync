﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;
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
    private readonly QbdAccessService _qbdAccessService;
    private readonly PopuliAccessService _populiAccessService;
    private readonly QbCustomerService _customerService;
    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ObservableCollection<StatusMessage> _statisticsMessages = new();

    [ObservableProperty] private string? _companyName = "";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel(
        MessageBoxService messageBoxService,
        QbdAccessService qbdAccessService,
        PopuliAccessService populiAccessService,
        QbCustomerService customerService
    )
    {
        _messageBoxService = messageBoxService;
        _qbdAccessService = qbdAccessService;
        _populiAccessService = populiAccessService;
        _customerService = customerService;

        _customerService.OnProgressChanged += ProgressChanged;
    }

    private void ProgressChanged(object? sender, PopToQbCustomerImportArgs args)
    {
        if (args.Data is Exception ex)
        {
            SetSyncStatusMessage($"{args.Status} : {ex.Message}", StatusMessageType.Error);
        }
        else if (args.Data is string msg)
        {
            SetSyncStatusMessage($"{args.Status}: {msg}", StatusMessageType.Info);
        }
        else
        {
            SetSyncStatusMessage($"{args.Status}", StatusMessageType.Info);
        }
    }

    [RelayCommand]
    private async Task ConnectToQb()
    {
        try
        {
            await Task.Run(() =>
            {
                if (_qbdAccessService.OpenConnection())
                {
                    CompanyName = _qbdAccessService.GetCompanyName();
                    SetSyncStatusMessage("Connected to QuickBooks.", StatusMessageType.Success);
                }
            });
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowError("Error", ex.Message);
            SetSyncStatusMessage("Failed to connect to QuickBooks.", StatusMessageType.Error);

            _qbdAccessService.CloseConnection();
            SetSyncStatusMessage("QuickBooks isn't running.", StatusMessageType.Info);
            _logger.Error(ex);
        }
    }

    [RelayCommand]
    private async Task StartSync()
    {
        try
        {
            SetSyncStatusMessage($"Populi to QBD Sync. Version: {VersionHelper.Version}", StatusMessageType.Info);
            SetSyncStatusMessage("Starting Sync.", StatusMessageType.Info);
            
            var persons = await _populiAccessService.GetAllPersonsAsync();
            if (persons != null)
            {
                SetSyncStatusMessage($"Fetched Persons : {persons.Count}", StatusMessageType.Success);
                var resp = await _customerService.AddCustomersAsync(persons);
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


    private void SetSyncStatusMessage(string? message, StatusMessageType type)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SyncStatusMessages.Add(new StatusMessage
            {
                Message = message,
                MessageType = type
            });
        });
    }

    private void SetStatisticsMessage(string? message, StatusMessageType type)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatisticsMessages.Add(new StatusMessage
            {
                Message = message,
                MessageType = type
            });
        });
    }

    public void Dispose()
    {
        // TODO release managed resources here
        _customerService.OnProgressChanged -= ProgressChanged;

    }
}