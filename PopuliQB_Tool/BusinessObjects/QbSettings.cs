using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PopuliQB_Tool.BusinessObjects;

public sealed partial class QbSettings : ObservableObject
{
    private static readonly Lazy<QbSettings> Lazy = new(() => new QbSettings());
    public static QbSettings Instance { get; set; } = Lazy.Value;

    [ObservableProperty] private DateTime _postedFrom = DateTime.UtcNow;
    [ObservableProperty] private DateTime _postedTo = DateTime.UtcNow;
    [ObservableProperty] private bool _applyPostedDateFilter;

    [ObservableProperty] private DateTime _addedFrom = DateTime.UtcNow;
    [ObservableProperty] private DateTime _addedTo = DateTime.UtcNow;
    [ObservableProperty] private bool _applyAddedDateFilter;

    [ObservableProperty] private string _numFrom = "";
    [ObservableProperty] private string _numTo = "";
    [ObservableProperty] private bool _applyNumFilter;

    [ObservableProperty] private PopPerson? _student;
    [ObservableProperty] private bool _applyStudentFilter;
    [ObservableProperty] private bool _applyIgnoreStartingBalanceFilter = true;
    [ObservableProperty] private bool _applyAidPaymentsAreCreditMemoFilter = true;
    [ObservableProperty] private string _syncStudentIds = "97113, 35196, 96420, 93831";
    [ObservableProperty] private bool _applySyncStudentIdsFilter = true;
    [ObservableProperty] private int _popConvenienceAccId = 74;
    [ObservableProperty] private string _skipStartingBalanceItemName = "Starting Balance";
    [ObservableProperty] private string _uniquePopuliIdName = "UniquePopuliId";
    [ObservableProperty] private string _appVersion = Assembly.GetExecutingAssembly()!.GetName()!.Version!.ToString();


    private QbSettings()
    {
    }
}