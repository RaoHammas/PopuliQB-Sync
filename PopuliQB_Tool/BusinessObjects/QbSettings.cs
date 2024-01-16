using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PopuliQB_Tool.BusinessObjects;

public sealed class QbSettings : INotifyPropertyChanged
{
    private static readonly Lazy<QbSettings> Lazy = new(() => new QbSettings());
    private DateTime _postedFrom = DateTime.UtcNow;
    private DateTime _postedTo = DateTime.UtcNow;
    private bool _applyPostedDateFilter;
    private DateTime _addedFrom = DateTime.UtcNow;
    private DateTime _addedTo = DateTime.UtcNow;
    private bool _applyAddedDateFilter;
    private string _invoiceNumFrom = "";
    private string _invoiceNumTo = "";
    private bool _applyInvoiceNumFilter;
    private PopPerson? _student;
    private bool _applyStudentFilter;
    private bool _applyIgnoreStartingBalanceFilter = true;
    private bool _applyAidPaymentsAreCreditMemoFilter = true;
    private string _syncStudentIds = "97113, 35196, 96420, 93831";
    private int _popConvenienceAccId = 74;
    private string _skipStartingBalanceItemName = "Starting Balance";

    public static QbSettings Instance => Lazy.Value;

    private QbSettings()
    {
    }

    public string SkipStartingBalanceItemName
    {
        get => _skipStartingBalanceItemName;
        set => SetField(ref _skipStartingBalanceItemName, value);
    }

    public int PopConvenienceAccId
    {
        get => _popConvenienceAccId;
        set => SetField(ref _popConvenienceAccId, value);
    }

    public string SyncStudentIds
    {
        get => _syncStudentIds;
        set => SetField(ref _syncStudentIds, value);
    }

    public DateTime PostedFrom
    {
        get => _postedFrom;
        set => SetField(ref _postedFrom, value);
    }

    public DateTime PostedTo
    {
        get => _postedTo;
        set => SetField(ref _postedTo, value);
    }

    public bool ApplyPostedDateFilter
    {
        get => _applyPostedDateFilter;
        set => SetField(ref _applyPostedDateFilter, value);
    }

    public DateTime AddedFrom
    {
        get => _addedFrom;
        set => SetField(ref _addedFrom, value);
    }

    public DateTime AddedTo
    {
        get => _addedTo;
        set => SetField(ref _addedTo, value);
    }

    public bool ApplyAddedDateFilter
    {
        get => _applyAddedDateFilter;
        set => SetField(ref _applyAddedDateFilter, value);
    }

    public string InvoiceNumFrom
    {
        get => _invoiceNumFrom;
        set => SetField(ref _invoiceNumFrom, value);
    }

    public string InvoiceNumTo
    {
        get => _invoiceNumTo;
        set => SetField(ref _invoiceNumTo, value);
    }

    public bool ApplyInvoiceNumFilter
    {
        get => _applyInvoiceNumFilter;
        set => SetField(ref _applyInvoiceNumFilter, value);
    }

    public PopPerson? Student
    {
        get => _student;
        set => SetField(ref _student, value);
    }

    public bool ApplyStudentFilter
    {
        get => _applyStudentFilter;
        set => SetField(ref _applyStudentFilter, value);
    }

    public bool ApplyIgnoreStartingBalanceFilter
    {
        get => _applyIgnoreStartingBalanceFilter;
        set => SetField(ref _applyIgnoreStartingBalanceFilter, value);
    }

    public bool ApplyAidPaymentsAreCreditMemoFilter
    {
        get => _applyAidPaymentsAreCreditMemoFilter;
        set => SetField(ref _applyAidPaymentsAreCreditMemoFilter, value);
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}