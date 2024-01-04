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
        var filter = new PopFilter
        {
            Page = page,
            Expand = new []{"addresses", "phone_numbers", "student"},
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ANY",
                    Fields = new List<PopFilterField>(),
                }
            }
        };

        filter.FilterItems[0].Fields.Add(new PopFilterField
        {
            Name = "student_id",
            Positive = "1",
            Value = new PopFilterValueTypeText()
            {
                Type = "IS",
                Text = "97113", //PERSON ID
            }
        });

        filter.FilterItems[0].Fields.Add(new PopFilterField
        {
            Name = "student_id",
            Positive = "1",
            Value = new PopFilterValueTypeText()
            {
                Type = "IS",
                Text = "35196", //PERSON ID
            }
        });

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
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

    
    public async Task<List<PopAidAwards>> FetchAllAStudentAwardsAsync(int studentId, string studentDisplayName)
    {
        var aidAwardsList = new List<PopAidAwards>();
        var page = 1;
        var resp = await GetAllStudentAwardsAsync(studentId, studentDisplayName, page);
        if (resp.Data != null)
        {
            aidAwardsList.AddRange(resp.Data);
        }
        while (resp.HasMore == true)
        {
            resp = await GetAllStudentAwardsAsync(studentId, studentDisplayName, ++page);
            if (resp.Data != null)
            {
                aidAwardsList.AddRange(resp.Data);
            }
        }

        return aidAwardsList;
    }

    private async Task<PopResponse<PopAidAwards>> GetAllStudentAwardsAsync(int studentId, string studentDisplayName, int page)
    {
        var request = new RestRequest($"{ProdUrl}/aidawards");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterField>(),
                }
            }
        };

        filter.FilterItems[0].Fields.Add(new PopFilterField
        {
            Name = "student",
            Positive = "1",
            Value = new PopFilterValue()
            {
                DisplayText = studentDisplayName,
                Id = studentId.ToString()
            }
        });

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopResponse<PopAidAwards>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Aid Awards. {@response}", response);
        return new();
    }


    public async Task<PopResponse<PopPayment>> GetAllStudentPaymentsAsync(int studentId)
    {
        var request = new RestRequest($"{ProdUrl}/people/{studentId}/payments/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        // var body = $@"{{""page"": {page}}}"; //from url

        var response =
            await _client.ExecuteAsync<PopResponse<PopPayment>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Invoices. {@response}", response);
        return new();
    }
}