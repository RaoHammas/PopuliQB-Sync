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

    public PopuliAccessService()
    {
        _client = new RestClient(new RestClientOptions
        {
            ThrowOnDeserializationError = false,
        });
    }

    public async Task<List<PopPerson>?> GetAllPersonsAsync()
    {
        var request = new RestRequest($"{DevUrl}people/");
        request.AddHeader("Authorization", $"Bearer {AuthToken}");
        request.AddHeader("Content-Type", "application/json");
        const string? body = """
                            {"expand": ["addresses", "phone_numbers", "student"]}
                            """;

        request.AddStringBody(body, DataFormat.Json);

        var response = await _client.ExecuteAsync<PopuliResponse<PopPerson>>(request, Method.Get, CancellationToken.None);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            return response.Data?.Data;
        }

        _logger.Error("Failed to fetch Persons. {@response}", response);
        return new();
    }
}