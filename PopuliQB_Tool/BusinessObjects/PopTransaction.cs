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

    [JsonPropertyName("reversed_by_id")] public int? ReversedById { get; set; }

    [JsonPropertyName("reposts")] public object? Reposts { get; set; }

    [JsonPropertyName("reposted_by_id")] public object? RepostedById { get; set; }

    [JsonPropertyName("ledger_entries")] public List<PopLedgerEntry> LedgerEntries { get; set; }
    [JsonPropertyName("report_data")] public PopTransReportData ReportData { get; set; }
}

public class PopTransReportData
{
    [JsonPropertyName("student_dummyid")] public string? StudentDummyid { get; set; }

    [JsonPropertyName("primary_actor")] public string? PrimaryActor { get; set; }

    [JsonPropertyName("term_name")] public string? TermName { get; set; }

    [JsonPropertyName("invoiceid")] public int? Invoiceid { get; set; }

    [JsonPropertyName("invoice_number")] public int? InvoiceNumber { get; set; }

    [JsonPropertyName("paymentid")] public int? Paymentid { get; set; }

    [JsonPropertyName("reference_number")] public string? ReferenceNumber { get; set; }

    [JsonPropertyName("payment_number")] public int? PaymentNumber { get; set; }

    [JsonPropertyName("payment_source_type")]
    public string? PaymentSourceType { get; set; }

    [JsonPropertyName("donation_id")] public object? DonationId { get; set; }

    [JsonPropertyName("donation_number")] public object? DonationNumber { get; set; }

    [JsonPropertyName("added_by_name")] public string? AddedByName { get; set; }

    [JsonPropertyName("voided_by_name")] public object? VoidedByName { get; set; }

    [JsonPropertyName("linked_transaction_id")]
    public object LinkedTransactionId { get; set; }

    [JsonPropertyName("linked_transaction_number")]
    public object? LinkedTransactionNumber { get; set; }

    [JsonPropertyName("payment_gateway_processing_fee")]
    public string? PaymentGatewayProcessingFee { get; set; }

    [JsonPropertyName("online_charge_method")]
    public string? OnlineChargeMethod { get; set; }

    [JsonPropertyName("check_number")] public object? CheckNumber { get; set; }

    [JsonPropertyName("check_batch_number")]
    public object? CheckBatchNumber { get; set; }

    [JsonPropertyName("inventory_batch_item_variation_name")]
    public object? InventoryBatchItemVariationName { get; set; }

    [JsonPropertyName("inventory_batch_item_id")]
    public object? InventoryBatchItemId { get; set; }

    [JsonPropertyName("inventory_batch_item_variation_id")]
    public object? InventoryBatchItemVariationId { get; set; }
}