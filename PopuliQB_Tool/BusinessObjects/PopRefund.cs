using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class PopRefund
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("academic_term_id")] public int? AcademicTermId { get; set; }

    [JsonPropertyName("student_id")] public int? StudentId { get; set; }

    [JsonPropertyName("aid_id")] public int? AidId { get; set; }

    [JsonPropertyName("aid_award_id")] public int? AidAwardId { get; set; }

    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("gross_amount")] public double? GrossAmount { get; set; }

    [JsonPropertyName("multiplier")] public object? Multiplier { get; set; }

    [JsonPropertyName("scheduled_date")] public DateTime? ScheduledDate { get; set; }

    [JsonPropertyName("posted_date")] public DateTime? PostedDate { get; set; }

    [JsonPropertyName("status_date")] public DateTime? StatusDate { get; set; }

    [JsonPropertyName("payment_id")] public int? PaymentId { get; set; }

    [JsonPropertyName("refund_id")] public int? RefundId { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("disbursement_number")]
    public int? DisbursementNumber { get; set; }

    [JsonPropertyName("sequence_number")] public object? SequenceNumber { get; set; }

    [JsonPropertyName("cod_status")] public string? CodStatus { get; set; }

    [JsonPropertyName("cod_amount")] public double? CodAmount { get; set; }

    [JsonPropertyName("cod_originated")] public object? CodOriginated { get; set; }

    [JsonPropertyName("cod_net_amount")] public double? CodNetAmount { get; set; }

    [JsonPropertyName("cod_released")] public object? CodReleased { get; set; }

    [JsonPropertyName("cod_needs_sync")] public object? CodNeedsSync { get; set; }

    [JsonPropertyName("cod_enrollment_status")]
    public object? CodEnrollmentStatus { get; set; }

    [JsonPropertyName("auto_calculated_enrollment")]
    public object? AutoCalculatedEnrollment { get; set; }

    [JsonPropertyName("enrollment_mismatch")]
    public object? EnrollmentMismatch { get; set; }

    [JsonPropertyName("cod_program_cip_code")]
    public object? CodProgramCipCode { get; set; }

    [JsonPropertyName("cod_payment_period_start_date")]
    public object? CodPaymentPeriodStartDate { get; set; }

    [JsonPropertyName("cod_payment_period_end_date")]
    public object? CodPaymentPeriodEndDate { get; set; }

    [JsonPropertyName("external_id")] public object? ExternalId { get; set; }

    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }

    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }

    [JsonPropertyName("report_data")] public RefundReportData ReportData { get; set; }
}

public class RefundReportData
{
    [JsonPropertyName("firstname")] public string? Firstname { get; set; }

    [JsonPropertyName("lastname")] public string? Lastname { get; set; }

    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }

    [JsonPropertyName("aid_name")] public string? AidName { get; set; }

    [JsonPropertyName("aid_type")] public string? AidType { get; set; }

    [JsonPropertyName("is_scholarship")] public int? IsScholarship { get; set; }

    [JsonPropertyName("aid_source")] public string? AidSource { get; set; }

    [JsonPropertyName("aid_abbrv")] public string? AidAbbrv { get; set; }

    [JsonPropertyName("term_name")] public string? TermName { get; set; }

    [JsonPropertyName("aid_year_id")] public int? AidYearId { get; set; }

    [JsonPropertyName("aid_year_name")] public string? AidYearName { get; set; }
}