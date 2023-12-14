using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PopuliQB_Tool.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private string _syncStatusMessages = "";
    [ObservableProperty] private string _statisticsMessages = "";
    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private DateTime _startTransDate = DateTime.Now;

    public MainWindowViewModel()
    {
    }

    [RelayCommand]
    private Task ConnectToQb()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task StartSync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}