using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class PopLedgerEntry
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("direction")] public string? Direction { get; set; }

    [JsonPropertyName("account_id")] public int? AccountId { get; set; }

    [JsonPropertyName("actor_type")] public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")] public int? ActorId { get; set; }

    [JsonPropertyName("debit")] public double? Debit { get; set; }

    [JsonPropertyName("credit")] public double? Credit { get; set; }

    [JsonPropertyName("invoice_item_id")] public object? InvoiceItemId { get; set; }

    [JsonPropertyName("fund_id")] public object? FundId { get; set; }

    [JsonPropertyName("is_deposit")] public object? IsDeposit { get; set; }
}

public class PopTransaction
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("number")] public int? Number { get; set; }

    [JsonPropertyName("posted_on")] public DateTime? PostedOn { get; set; }

    [JsonPropertyName("actor_type")] public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")] public int? ActorId { get; set; }

    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }

    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }

    [JsonPropertyName("voided_at")] public object? VoidedAt { get; set; }

    [JsonPropertyName("voided_by_id")] public object? VoidedById { get; set; }

    [JsonPropertyName("link_type")] public string? LinkType { get; set; }

    [JsonPropertyName("link_id")] public int? LinkId { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("reverses")] public object? Reverses { get; set; }

    [JsonPropertyName("reversed_by_id")] public object? ReversedById { get; set; }

    [JsonPropertyName("reposts")] public object? Reposts { get; set; }

    [JsonPropertyName("reposted_by_id")] public object? RepostedById { get; set; }

    [JsonPropertyName("ledger_entries")] public List<PopLedgerEntry> LedgerEntries { get; set; }
}