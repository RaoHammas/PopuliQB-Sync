using CommunityToolkit.Mvvm.ComponentModel;

namespace PopuliQB_Tool.Models;

public partial class StatusMessage : ObservableObject
{
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private StatusMessageType _messageType = StatusMessageType.Info;
}

public enum StatusMessageType
{
    Error,
    Success,
    Info,
    Warn,
    All,
}