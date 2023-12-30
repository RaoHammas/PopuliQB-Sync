using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NLog;
using PopuliQB_Tool.BusinessObjects;
using RestSharp;
using System.Windows.Controls;
using PopuliQB_Tool.PopuliFilters;

namespace PopuliQB_Tool.BusinessServices;

public class PopuliAccessService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private const string? DevUrl = "https://divinemercyedu-validation.populi.co/api2";
    private const string? ProdUrl = "https://divinemercyedu.populiweb.com/api2";

    private const string? AuthToken =
        "sk_y75vZUhN4fP14zXrGcnl8ThHDiLf0xAdGVLWekgcbvgKN8KJHcEw7y0JOp8YlrZ4ZtNSRahQGnkK8dhHFsHNyG";

    private readonly RestClient _client;
    public List<PopPerson> AllPopuliPersons { get; set; } = new();
    public List<PopInvoice> AllPopuliInvoices { get; set; } = new();
    public List<PopAccount> AllPopuliAccounts { get; set; } = new();

    public PopuliAccessService()
    {
        _client = new RestClient(new RestClientOptions
        {
            ThrowOnDeserializationError = false,
        });
    }

    public async Task<PopResponse<PopPerson>> GetAllPersonsAsync(int page = 1)
    {

        var request = new RestRequest($"{ProdUrl}/people/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        // var body = $@"{{""expand"": [""addresses"", ""phone_numbers"", ""student""], ""page"": {page}}}"; //from name
        var body = """
                   {
                     "expand": ["addresses", "phone_numbers", "student"],
                       "filter":
                       {
                           "0": {
                               "logic": "ALL",
                               "fields": [
                                   {
                                       "name": "student_id",
                                       "value": {
                                           "type": "IS",
                                           "text": "97113"
                                       },
                                       "positive": 1
                                   }
                               ]
                           }
                       }
                   }
                   """;
        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopResponse<PopPerson>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                AllPopuliPersons.AddRange(response.Data.Data);
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Persons. {@response}", response);
        return new();
    }

    public async Task<PopResponse<PopInvoice>> GetAllInvoicesAsync(int page = 1)
    {
        var request = new RestRequest($"{ProdUrl}/invoices/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        // var body = $@"{{""page"": {page}}}"; //from url

        var filter = new PopFilter
        {
            Expand = new[] { "payments", "credits" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterField>(),
                }
            }
        };

        if (QbSettings.Instance.ApplyPostedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterField
            {
                Name = "posted_date",
                Positive = "1",
                Value = new PopFilterValueRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.PostedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.PostedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        if (QbSettings.Instance.ApplyAddedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterField
            {
                Name = "added_time",
                Positive = "1",
                Value = new PopFilterValueRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.AddedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.AddedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        if (QbSettings.Instance.ApplyInvoiceNumFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterField
            {
                Name = "invoice_number",
                Positive = "1",
                Value = new PopFilterValueRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.InvoiceNumFrom,
                    End = QbSettings.Instance.InvoiceNumTo,
                }
            });
        }

        if (QbSettings.Instance.ApplyStudentFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterField
            {
                Name = "student",
                Positive = "1",
                Value = new PopFilterValue()
                {
                    DisplayText = QbSettings.Instance.Student.DisplayName!,
                    Id = QbSettings.Instance.Student.Id!.Value.ToString()
                }
            });
        }

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopResponse<PopInvoice>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                AllPopuliInvoices.AddRange(response.Data.Data!);
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Invoices. {@response}", response);
        return new();
    }

    public async Task<PopTransaction?> GetTransactionWithLedgerAsync(int transactionId)
    {
        var request = new RestRequest($"{ProdUrl}/transactions/{transactionId}");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = """
                   {
                       "expand": ["ledger_entries"]
                   }
                   """;

        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopTransaction>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Transaction. {@response}", response);
        return null;
    }

    public async Task SyncAllAccountsAsync(int page = 1)
    {
        AllPopuliAccounts = new List<PopAccount>();
        var resp = await GetAllAccountsAsync(1);
        while (resp.HasMore == true)
        {
            await GetAllAccountsAsync(++page);
        }

        foreach (var account in AllPopuliAccounts)
        {
            // because populi gives account like this  "account_number": "120010 ·"
            if (account.AccountNumber != null)
            {
                account.AccountNumber = account.AccountNumber.Split(" ")[0];
            }
        }
    }

    private async Task<PopResponse<PopAccount>> GetAllAccountsAsync(int page)
    {
        var request = new RestRequest($"{ProdUrl}/accounts");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");
        var body = $@"{{""page"": {page}}}";
        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopResponse<PopAccount>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                AllPopuliAccounts.AddRange(response.Data.Data!);
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Accounts. {@response}", response);
        return new();
    }
}