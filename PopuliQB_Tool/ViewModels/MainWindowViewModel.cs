using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.Helpers;
using PopuliQB_Tool.Models;
using PopuliQB_Tool.Services;

namespace PopuliQB_Tool.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly MessageBoxService _messageBoxService;
    private readonly QbdAccessService _qbdAccessService;
    [ObservableProperty] private ObservableCollection<StatusMessage> _syncStatusMessages = new();
    [ObservableProperty] private ObservableCollection<StatusMessage> _statisticsMessages = new();

    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel(
        MessageBoxService messageBoxService
        , QbdAccessService qbdAccessService)
    {
        _messageBoxService = messageBoxService;
        _qbdAccessService = qbdAccessService;
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
        }
    }

    [RelayCommand]
    private Task StartSync()
    {
        try
        {
            SetSyncStatusMessage($"Populi to QBD Sync. Version: {VersionHelper.Version}", StatusMessageType.Info);


        }
        catch (Exception ex)
        {
            
        }
        return Task.CompletedTask;
    }


    private void SetSyncStatusMessage(string message, StatusMessageType type)
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

    private void SetStatisticsMessage(string message, StatusMessageType type)
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
    }
}