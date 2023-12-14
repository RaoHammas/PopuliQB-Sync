using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PopuliQB_Tool.BusinessServices;
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
        ,QbdAccessService qbdAccessService)
    {
        _messageBoxService = messageBoxService;
        _qbdAccessService = qbdAccessService;
    }

    [RelayCommand]
    private Task ConnectToQb()
    {
        try
        {
            if (_qbdAccessService.OpenConnection())
            {
                CompanyName = _qbdAccessService.GetCompanyName();
                SetSyncStatusMessage("Connected to QuickBooks.", StatusMessageType.Success);
            }

        }
        catch (Exception ex)
        {
            _messageBoxService.ShowError("Error", ex.Message);
            SetSyncStatusMessage("Failed to connect to QuickBooks.", StatusMessageType.Error);

            _qbdAccessService.CloseConnection();
            SetSyncStatusMessage("QuickBooks isn't running.", StatusMessageType.Info);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task StartSync()
    {
        return Task.CompletedTask;
    }


    private void SetSyncStatusMessage(string message, StatusMessageType type)
    {
        SyncStatusMessages.Add(new StatusMessage
        {
            Message = message,
            MessageType = type
        });
    }

    private void SetStatisticsMessage(string message, StatusMessageType type)
    {
        StatisticsMessages.Add(new StatusMessage
        {
            Message = message,
            MessageType = type
        });
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}