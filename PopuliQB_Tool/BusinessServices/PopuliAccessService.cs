using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.Messaging;
using NLog;
using PopuliQB_Tool.BusinessObjects;
using RestSharp;
using PopuliQB_Tool.PopuliFilters;
using PopuliQB_Tool.EventArgs;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class PopuliAccessService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly int _reqDelayMs;
    private readonly string? _url;
    private readonly string? _authToken;

    private readonly RestClient _client;
    public List<PopPerson> AllPopuliPersons { get; set; } = new();
    public List<PopInvoice> AllPopuliInvoices { get; set; } = new();
    public List<PopAccount> AllPopuliAccounts { get; set; } = new();
    public static List<PopDegree> AllPopuliDegrees { get; set; } = new();

    public PopuliAccessService(AppConfiguration appConfiguration)
    {
        _url = appConfiguration["API_URL"];
        _authToken = appConfiguration["TOKEN"];
        _reqDelayMs = Convert.ToInt32(appConfiguration["RequestDelayMs"]);

        _client = new RestClient(new RestClientOptions
        {
            ThrowOnDeserializationError = false,
            ThrowOnAnyError = false,
        });
    }

    #region STUDENTS

    public async Task<PopResponse<PopPerson>> GetAllPersonsAsync(int page = 1)
    {
        var request = new RestRequest($"{_url}/people/");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            Expand = new[] { "addresses", "phone_numbers", "student", "student_degrees" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>
                    {
                        new PopFilterTypeField
                        {
                            Name = "role",
                            Positive = "1",
                            Value = new PopFilterValueStatusId
                            {
                                Id = "5", // Role = Student
                                Status = "ACTIVE",
                            }
                        }
                    },
                }
            }
        };

        if (QbSettings.Instance.ApplySyncStudentIdsFilter)
        {
            filter.FilterItems.Add(
                new()
                {
                    Logic = "ANY",
                    Fields = new List<PopFilterTypeField>(),
                });

            var ids = QbSettings.Instance.SyncStudentIds.Split(',');
            foreach (var id in ids)
            {
                filter.FilterItems[1].Fields.Add(new PopFilterTypeField
                {
                    Name = "student_id",
                    Positive = "1",
                    Value = new PopFilterValueText()
                    {
                        Type = "IS",
                        Text = id.Trim(), //PERSON ID
                    }
                });
            }
        }

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopPerson>>(request);

        if (response is { Data: not null })
        {
            AllPopuliPersons.AddRange(response.Data);
            return response;
        }

        _logger.Error("Failed to fetch Persons. {@response}", response);
        return new();
    }

    #endregion

    #region ACCOUNTS

    public async Task SyncAllAccountsAsync(int page = 1)
    {
        AllPopuliAccounts = new List<PopAccount>();
        var resp = await FetchAllAccountsAsync(1);
        while (resp.HasMore == true)
        {
            await FetchAllAccountsAsync(++page);
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

    private async Task<PopResponse<PopAccount>> FetchAllAccountsAsync(int page)
    {
        var request = new RestRequest($"{_url}/accounts");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");
        var body = $@"{{""page"": {page}}}";
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopAccount>>(request);

        if (response is { Data: not null })
        {
            AllPopuliAccounts.AddRange(response.Data!);
            return response;
        }

        _logger.Error("Failed to fetch Accounts. {@response}", response);
        return new();
    }

    #endregion

    #region DEGREES

    public async Task<List<PopDegree>> GetAllStudentDegreesAsync()
    {
        AllPopuliDegrees = new List<PopDegree>();
        var page = 1;
        var resp = await FetchAllDegreesAsync(page);
        if (resp.Data != null)
        {
            AllPopuliDegrees.AddRange(resp.Data);
        }

        while (resp.HasMore == true)
        {
            resp = await FetchAllDegreesAsync(++page);
            if (resp.Data != null)
            {
                AllPopuliDegrees.AddRange(resp.Data);
            }
        }

        return AllPopuliDegrees;
    }

    private async Task<PopResponse<PopDegree>> FetchAllDegreesAsync(int page)
    {
        var request = new RestRequest($"{_url}/degrees");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");
        var body = $@"{{""page"": {page}}}";
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopDegree>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Degrees. {@response}", response);
        return new();
    }

    #endregion

    #region AWARDS

    public async Task<List<PopAidAwards>> GetAllStudentAwardsAsync(int studentId, string studentDisplayName)
    {
        var aidAwardsList = new List<PopAidAwards>();
        var page = 1;
        var resp = await FetchAllStudentAwardsAsync(studentId, studentDisplayName, page);
        if (resp.Data != null)
        {
            aidAwardsList.AddRange(resp.Data);
        }

        while (resp.HasMore == true)
        {
            resp = await FetchAllStudentAwardsAsync(studentId, studentDisplayName, ++page);
            if (resp.Data != null)
            {
                aidAwardsList.AddRange(resp.Data);
            }
        }

        return aidAwardsList;
    }

    private async Task<PopResponse<PopAidAwards>> FetchAllStudentAwardsAsync(int studentId, string studentDisplayName,
        int page)
    {
        var request = new RestRequest($"{_url}/aidawards");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>(),
                }
            }
        };

        filter.FilterItems[0].Fields.Add(new PopFilterTypeField
        {
            Name = "student",
            Positive = "1",
            Value = new PopFilterValueDisplayName()
            {
                DisplayText = studentDisplayName,
                Id = studentId.ToString()
            }
        });

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopAidAwards>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Aid Awards. {@response}", response);
        return new();
    }

    #endregion

    #region PAYMENTS

    public async Task<List<PopPayment>> GetAllStudentPaymentsAsync(int studentId)
    {
        var dataList = new List<PopPayment>();
        var page = 1;
        var resp = await FetchAllStudentPaymentsAsync(studentId, page);
        if (resp.Data != null)
        {
            dataList.AddRange(resp.Data);
        }

        while (resp.HasMore == true)
        {
            resp = await FetchAllStudentPaymentsAsync(studentId, ++page);
            if (resp.Data != null)
            {
                dataList.AddRange(resp.Data);
            }
        }

        return dataList;
    }

    private async Task<PopResponse<PopPayment>> FetchAllStudentPaymentsAsync(int studentId, int page)
    {
        var request = new RestRequest($"{_url}/people/{studentId}/payments/");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = $@"{{""page"": {page}}}";
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopPayment>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Payments. {@response}", response);
        return new();
    }

    #endregion

    #region TRANSACTIONS

    public async Task<List<PopTransaction>> GetAllStudentTransactionsAsync(int personId, string displayName)
    {
        var page = 0;
        var allData = new List<PopTransaction>();
        var hasMore = true;
        while (hasMore)
        {
            var data = await FetchAllStudentTransactionsAsync(personId, displayName, ++page);
            if (data.Data != null)
            {
                allData.AddRange(data.Data);
            }

            hasMore = data.HasMore ?? false;
        }

        return allData;
    }

    private async Task<PopResponse<PopTransaction>> FetchAllStudentTransactionsAsync(int personId, string displayName,
        int page)
    {
        var request = new RestRequest($"{_url}/transactions");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            Expand = new[] { "ledger_entries" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>(),
                }
            }
        };

        filter.FilterItems[0].Fields.Add(new PopFilterTypeField
        {
            Name = "primary_actor",
            Positive = "1",
            Value = new PopFilterValueDisplayName()
            {
                DisplayText = displayName,
                Id = personId.ToString()
            }
        });

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopTransaction>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Transactions. {@response}", response);
        return new();
    }

    #endregion

    #region INVOICES

    public async Task<PopResponse<PopInvoice>> GetAllInvoicesAsync(int page = 1)
    {
        var request = new RestRequest($"{_url}/invoices/");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
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
                    Fields = new List<PopFilterTypeField>(),
                }
            }
        };

        if (QbSettings.Instance.ApplyPostedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "posted_date",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.PostedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.PostedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        if (QbSettings.Instance.ApplyAddedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "added_time",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.AddedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.AddedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        if (QbSettings.Instance.ApplyNumFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "invoice_number",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.NumFrom,
                    End = QbSettings.Instance.NumTo,
                }
            });
        }

        if (QbSettings.Instance.ApplyStudentFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "student",
                Positive = "1",
                Value = new PopFilterValueDisplayName()
                {
                    DisplayText = QbSettings.Instance.Student.DisplayName!,
                    Id = QbSettings.Instance.Student.Id!.Value.ToString()
                }
            });
        }

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopInvoice>>(request);

        if (response is { Data: not null })
        {
            AllPopuliInvoices.AddRange(response.Data);
            return response;
        }

        _logger.Error("Failed to fetch Invoices. {@response}", response);
        return new();
    }

    public async Task<List<PopInvoice>> GetAllStudentInvoicesAsync(int personId, string displayName)
    {
        var page = 0;
        var allData = new List<PopInvoice>();
        var hasMore = true;
        while (hasMore)
        {
            var data = await FetchAllStudentInvoicesAsync(personId, displayName, ++page);
            if (data.Data != null)
            {
                allData.AddRange(data.Data);
            }

            hasMore = data.HasMore ?? false;
        }

        return allData;
    }

    private async Task<PopResponse<PopInvoice>> FetchAllStudentInvoicesAsync(int personId, string displayName, int page)
    {
        var request = new RestRequest($"{_url}/invoices");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            Expand = new[] { "credits" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>
                    {
                        new()
                        {
                            Name = "student",
                            Positive = "1",
                            Value = new PopFilterValueDisplayName
                            {
                                DisplayText = displayName,
                                Id = personId.ToString()
                            }
                        }
                    },
                }
            }
        };

        if (QbSettings.Instance.ApplyPostedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "posted_date",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.PostedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.PostedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        /*if (QbSettings.Instance.ApplyNumFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "invoice_number",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.NumFrom,
                    End = QbSettings.Instance.NumTo,
                }
            });
        }*/

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopInvoice>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Invoices. {@response}", response);
        return new PopResponse<PopInvoice>();
    }

    #endregion


    #region GET BY ID METHODS

    public async Task<PopCustomerRefund> GetCustomerRefundByPaymentIdAsync(int paymentId)
    {
        var request = new RestRequest($"{_url}/refunds/{paymentId}");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var response = await ExecuteRequestAsync<PopCustomerRefund>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Customer refund. {@response}", response);
        return new();
    }

    public async Task<PopAidType> GetAidTypeByIdAsync(int aidTypeId)
    {
        var request = new RestRequest($"{_url}/aidtypes/{aidTypeId}");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var response = await ExecuteRequestAsync<PopAidType>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch AidType. {@response}", response);
        return new();
    }

    public async Task<PopInvoice> GetInvoiceByIdAsync(int invoiceId)
    {
        var request = new RestRequest($"{_url}/invoices/{invoiceId}");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Expand = new[] { "credits" },
        };

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopInvoice>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Invoice. {@response}", response);
        return new();
    }

    public async Task<PopPayment> GetPaymentByIdAsync(int paymentId)
    {
        var request = new RestRequest($"{_url}/payments/{paymentId}");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var response = await ExecuteRequestAsync<PopPayment>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Payment. {@response}", response);
        return new();
    }

    public async Task<PopTransaction> GetTransactionByIdWithLedgerAsync(int transactionId)
    {
        var request = new RestRequest($"{_url}/transactions/{transactionId}");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = """
                   {
                       "expand": ["ledger_entries"]
                   }
                   """;

        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopTransaction>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Transaction. {@response}", response);
        return new();
    }

    #endregion

    #region REFUNDS

    public async Task<List<PopRefund>> GetAllStudentRefundsAsync(int personId, string displayName)
    {
        var page = 0;
        var allData = new List<PopRefund>();
        var hasMore = true;
        while (hasMore)
        {
            var data = await FetchAllStudentRefundsAsync(personId, displayName, ++page);
            if (data.Data != null)
            {
                allData.AddRange(data.Data);
            }

            hasMore = data.HasMore ?? false;
        }

        return allData;
    }

    private async Task<PopResponse<PopRefund>> FetchAllStudentRefundsAsync(int personId, string displayName,
        int page)
    {
        var request = new RestRequest($"{_url}/aiddisbursements");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            Expand = new[] { "ledger_entries" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>
                    {
                        new()
                        {
                            Name = "student",
                            Positive = "1",
                            Value = new PopFilterValueDisplayName()
                            {
                                DisplayText = displayName,
                                Id = personId.ToString()
                            }
                        }
                    },
                },
                new()
                {
                    Logic = "ANY",
                    Fields = new List<PopFilterTypeField>
                    {
                        new()
                        {
                            Name = "type",
                            Positive = "1",
                            Value = "SCHOLARSHIP",
                        },
                        new()
                        {
                            Name = "type",
                            Positive = "1",
                            Value = "GRANT",
                        },
                        new()
                        {
                            Name = "type",
                            Positive = "1",
                            Value = "LOAN",
                        },
                    },
                },
                new()
                {
                    Logic = "ANY",
                    Fields = new List<PopFilterTypeField>
                    {
                        new()
                        {
                            Name = "disbursement_type",
                            Positive = "1",
                            Value = "REFUND_TO_STUDENT",
                        },
                        new()
                        {
                            Name = "disbursement_type",
                            Positive = "1",
                            Value = "REFUND_TO_SOURCE",
                        }
                    },
                },
            }
        };

        if (QbSettings.Instance.ApplyPostedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "date",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.PostedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.PostedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopRefund>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Refunds. {@response}", response);
        return new();
    }

    #endregion

    #region CREDITS

    public async Task<List<PopCredit>> GetAllStudentCreditsAsync(int personId, string displayName)
    {
        var page = 0;
        var allData = new List<PopCredit>();
        var hasMore = true;
        while (hasMore)
        {
            var data = await FetchAllStudentCreditsAsync(personId, displayName, ++page);
            if (data.Data != null)
            {
                allData.AddRange(data.Data);
            }

            hasMore = data.HasMore ?? false;
        }

        return allData;
    }

    private async Task<PopResponse<PopCredit>> FetchAllStudentCreditsAsync(int personId, string displayName, int page)
    {
        var request = new RestRequest($"{_url}/credits");
        request.AddHeader("Authorization", $"Bearer {_authToken}");
        request.AddHeader("Content-Type", "application/json");

        var filter = new PopFilter
        {
            Page = page,
            //Expand = new[] { "credits" },
            FilterItems = new List<PopFilterItem>
            {
                new()
                {
                    Logic = "ALL",
                    Fields = new List<PopFilterTypeField>
                    {
                        new()
                        {
                            Name = "student",
                            Positive = "1",
                            Value = new PopFilterValueDisplayName
                            {
                                DisplayText = displayName,
                                Id = personId.ToString()
                            }
                        }
                    },
                }
            }
        };

        if (QbSettings.Instance.ApplyPostedDateFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "posted_date",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.PostedFrom.Date.ToString("yyyy-MM-dd"),
                    End = QbSettings.Instance.PostedTo.Date.ToString("yyyy-MM-dd"),
                }
            });
        }

        /*if (QbSettings.Instance.ApplyNumFilter)
        {
            filter.FilterItems[0].Fields.Add(new PopFilterTypeField
            {
                Name = "invoice_number",
                Positive = "1",
                Value = new PopFilterValueDateRange
                {
                    Type = "RANGE",
                    Start = QbSettings.Instance.NumFrom,
                    End = QbSettings.Instance.NumTo,
                }
            });
        }*/

        var body = JsonSerializer.Serialize(filter, new JsonSerializerOptions { WriteIndented = true });
        request.AddStringBody(body, DataFormat.Json);

        var response = await ExecuteRequestAsync<PopResponse<PopCredit>>(request);

        if (response != null)
        {
            return response;
        }

        _logger.Error("Failed to fetch Credits. {@response}", response);
        return new PopResponse<PopCredit>();
    }

    #endregion

    private async Task<T?> ExecuteRequestAsync<T>(RestRequest request)
    {
        try
        {
            await Task.Delay(_reqDelayMs);

            var response = await _client.ExecuteAsync(request, Method.Get, CancellationToken.None);

            if (response is { IsSuccessStatusCode: true, Content: not null })
            {
                var decodedContent = HttpUtility.HtmlDecode(response.Content);
                var responseData = JsonSerializer.Deserialize<T>(decodedContent, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                });

                if (responseData != null)
                {
                    return responseData;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed while executing request. {@ex}", ex.ToString());
            WeakReferenceMessenger.Default.Send(new ErrorMessage(ex, ex.Message));
        }


        return default;
    }
}