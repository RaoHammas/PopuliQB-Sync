using NLog;
using PopuliQB_Tool.BusinessObjects;
using RestSharp;

namespace PopuliQB_Tool.BusinessServices;

public class PopuliAccessService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private const string? DevUrl = "https://divinemercyedu-validation.populi.co/api2/";
    private const string? ProdUrl = "https://divinemercyedu.populiweb.com/api2/";

    private const string? AuthToken =
        "sk_y75vZUhN4fP14zXrGcnl8ThHDiLf0xAdGVLWekgcbvgKN8KJHcEw7y0JOp8YlrZ4ZtNSRahQGnkK8dhHFsHNyG";

    private readonly RestClient _client;
    public List<PopPerson> AllPopuliPersons { get; set; } = new();
    public List<PopInvoice> AllPopuliInvoices { get; set; } = new();

    public PopuliAccessService()
    {
        _client = new RestClient(new RestClientOptions
        {
            ThrowOnDeserializationError = false,
        });
    }

    public async Task<PopuliResponse<PopPerson>> GetAllPersonsAsync(int page = 1)
    {
        var request = new RestRequest($"{ProdUrl}people/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = $@"{{""expand"": [""addresses"", ""phone_numbers"", ""student""], ""page"": {page}}}";

        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopuliResponse<PopPerson>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                AllPopuliPersons = response.Data.Data;
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Persons. {@response}", response);
        return new();
    }

    public async Task<PopuliResponse<PopInvoice>> GetAllInvoicesAsync(int page = 1)
    {
        var request = new RestRequest($"{ProdUrl}invoices/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = $@"{{""page"": {page}}}";

        request.AddStringBody(body, DataFormat.Json);

        var response =
            await _client.ExecuteAsync<PopuliResponse<PopInvoice>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            if (response.Data != null)
            {
                AllPopuliInvoices = response.Data.Data;
                return response.Data;
            }
        }

        _logger.Error("Failed to fetch Persons. {@response}", response);
        return new();
    }
}