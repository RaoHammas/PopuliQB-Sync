﻿using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PopuliQB_Tool.BusinessServices;

namespace PopuliQB_Tool.BusinessObjects;

public sealed partial class QbSettings : ObservableObject
{
    private readonly PopuliAccessService _populiAccessService;

    private static readonly Lazy<QbSettings> Lazy = new(() =>
        new QbSettings(App.Services.GetRequiredService<PopuliAccessService>()));

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
    [ObservableProperty] private bool _applySyncStudentIdsFilter = false;
    [ObservableProperty] private int _popConvenienceAccId = 74;
    [ObservableProperty] private string _skipStartingBalanceItemName = "Starting Balance";
    [ObservableProperty] private string _uniquePopuliIdName = "UniquePopuliId";
    [ObservableProperty] private string _appVersion = Assembly.GetExecutingAssembly()!.GetName()!.Version!.ToString();

    public Func<QBCustomer, string, string, bool> CustomerPredicate = (customer, firstName, lastName)
        => string.Equals(customer.QbCustomerFName!.Trim(), firstName.Trim(), StringComparison.CurrentCultureIgnoreCase)
           && string.Equals(customer.QbCustomerLName!.Trim(), lastName.Trim(),
               StringComparison.CurrentCultureIgnoreCase);

    public int GetPopuliAccountReceivableId(List<PopLedgerEntry> entries)
    {
        foreach (var nonConvEntry in entries)
        {
            if (nonConvEntry.Credit > 0)
            {
                var acc = _populiAccessService.AllPopuliAccounts.FirstOrDefault(x => x.Id == nonConvEntry.AccountId);
                if (acc != null && acc.Type?.ToLower() == "Asset".ToLower())
                {
                    return nonConvEntry.AccountId ?? 0;
                }
            }
        }

        return entries.First(x => x.Direction == "debit").AccountId ?? 0;
    }

    private QbSettings(PopuliAccessService populiAccessService)
    {
        _populiAccessService = populiAccessService;
    }
}