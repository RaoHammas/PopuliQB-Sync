using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopAccount
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    public string? QbAccountListId { get; set; }
}