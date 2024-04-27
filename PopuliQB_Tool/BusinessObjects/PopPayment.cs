using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopPayment
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("student_id")] public int? StudentId { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("number")] public object? Number { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("online_payment_id")]
    public object? OnlinePaymentId { get; set; }

    [JsonPropertyName("convenience_fee_amount")]
    public double? ConvenienceFeeAmount { get; set; }

    [JsonPropertyName("paid_by_type")] public string? PaidByType { get; set; }

    [JsonPropertyName("paid_by_id")] public int? PaidById { get; set; }

    [JsonPropertyName("aid_type_id")] public int? AidTypeId { get; set; }

    [JsonPropertyName("refund_source")] public string? RefundSource { get; set; }

    [JsonPropertyName("reference_number")] public object? ReferenceNumber { get; set; }

    [JsonPropertyName("receipt_number")] public string? ReceiptNumber { get; set; }

    [JsonPropertyName("amount_available")] public double? AmountAvailable { get; set; }

    [JsonPropertyName("currency")] public string? Currency { get; set; }

    [JsonPropertyName("exchange_rate")] public object? ExchangeRate { get; set; }

    [JsonPropertyName("home_currency_amount")]
    public object? HomeCurrencyAmount { get; set; }

    [JsonPropertyName("recurring_money_transfer_id")]
    public object? RecurringMoneyTransferId { get; set; }

    [JsonPropertyName("aid_disbursement_id")]
    public object? AidDisbursementId { get; set; }

    [JsonPropertyName("treat_as_aid")] public bool? TreatAsAid { get; set; }

    [JsonPropertyName("organization_name")]
    public object? OrganizationName { get; set; }

    [JsonPropertyName("method")] public object? Method { get; set; }
}