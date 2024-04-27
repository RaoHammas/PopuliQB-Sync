using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopAidType
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("startyear")]
    public object? Startyear { get; set; }

    [JsonPropertyName("endyear")]
    public object? Endyear { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbrv")]
    public string? Abbrv { get; set; }

    [JsonPropertyName("cod_abbrv")]
    public object? CodAbbrv { get; set; }

    [JsonPropertyName("title_iv")]
    public bool? TitleIv { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("is_scholarship")]
    public bool? IsScholarship { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("multiplier")]
    public object? Multiplier { get; set; }

    [JsonPropertyName("federal_aid_id")]
    public object? FederalAidId { get; set; }

    [JsonPropertyName("need_based")]
    public bool? NeedBased { get; set; }

    [JsonPropertyName("count_against_need")]
    public bool? CountAgainstNeed { get; set; }

    [JsonPropertyName("report_on_1098t")]
    public bool? ReportOn1098t { get; set; }

    [JsonPropertyName("non_eligible_fee_aid")]
    public bool? NonEligibleFeeAid { get; set; }

    [JsonPropertyName("count_as_tuition_discount")]
    public bool? CountAsTuitionDiscount { get; set; }

    [JsonPropertyName("counts_as_efa")]
    public bool? CountsAsEfa { get; set; }

    [JsonPropertyName("only_allow_whole_dollar_amounts")]
    public bool? OnlyAllowWholeDollarAmounts { get; set; }

    [JsonPropertyName("allow_partial_acceptance")]
    public bool? AllowPartialAcceptance { get; set; }

    [JsonPropertyName("require_enrollment_verification")]
    public bool? RequireEnrollmentVerification { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("external_id")]
    public object ExternalId { get; set; }

    [JsonPropertyName("sandbox")]
    public bool? Sandbox { get; set; }
}