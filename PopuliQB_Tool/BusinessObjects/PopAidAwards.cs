using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopAidAwards
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("student_id")] public int? StudentId { get; set; }

    [JsonPropertyName("aid_year_id")] public int? AidYearId { get; set; }

    [JsonPropertyName("aid_type_id")] public int? AidTypeId { get; set; }

    /*[JsonPropertyName("sequence_number")] public int? SequenceNumber { get; set; }

    [JsonPropertyName("original_accepted_amount")]
    public int? OriginalAcceptedAmount { get; set; }

    [JsonPropertyName("award_limit")] public int? AwardLimit { get; set; }

    [JsonPropertyName("max_amount")] public int? MaxAmount { get; set; }

    [JsonPropertyName("net_amount")] public int? NetAmount { get; set; }

    [JsonPropertyName("multiplier")] public double? Multiplier { get; set; }

    [JsonPropertyName("auto_calc_percent_amount")]
    public bool? AutoCalcPercentAmount { get; set; }

    [JsonPropertyName("fee_percent")] public int? FeePercent { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("loan_booked_on")] public object? LoanBookedOn { get; set; }

    [JsonPropertyName("loan_payment_to_servicer_amount")]
    public int? LoanPaymentToServicerAmount { get; set; }

    [JsonPropertyName("loan_payment_to_servicer_date")]
    public object LoanPaymentToServicerDate { get; set; }

    [JsonPropertyName("plus_loan_has_endorser")]
    public bool? PlusLoanHasEndorser { get; set; }

    [JsonPropertyName("plus_loan_endorser_amount")]
    public object? PlusLoanEndorserAmount { get; set; }

    [JsonPropertyName("plus_loan_credit_requirements_met")]
    public bool? PlusLoanCreditRequirementsMet { get; set; }

    [JsonPropertyName("loan_booked_cod_response_id")]
    public object? LoanBookedCodResponseId { get; set; }

    [JsonPropertyName("year_coa")] public object? YearCoa { get; set; }

    [JsonPropertyName("cod_id")] public object? CodId { get; set; }

    [JsonPropertyName("cod_amount")] public int? CodAmount { get; set; }

    [JsonPropertyName("cod_origination_fee_percent")]
    public object? CodOriginationFeePercent { get; set; }

    [JsonPropertyName("cod_start_date")] public object? CodStartDate { get; set; }

    [JsonPropertyName("cod_end_date")] public object? CodEndDate { get; set; }

    [JsonPropertyName("cod_student_level_code")]
    public object? CodStudentLevelCode { get; set; }

    [JsonPropertyName("cod_academic_year_start_date")]
    public object? CodAcademicYearStartDate { get; set; }

    [JsonPropertyName("cod_academic_year_end_date")]
    public object CodAcademicYearEndDate { get; set; }

    [JsonPropertyName("cod_program_length")]
    public object? CodProgramLength { get; set; }

    [JsonPropertyName("cod_program_length_units")]
    public object? CodProgramLengthUnits { get; set; }

    [JsonPropertyName("cod_program_credential_level")]
    public object? CodProgramCredentialLevel { get; set; }

    [JsonPropertyName("cod_special_program")]
    public object? CodSpecialProgram { get; set; }

    [JsonPropertyName("cod_weeks_programs_academic_year")]
    public object? CodWeeksProgramsAcademicYear { get; set; }

    [JsonPropertyName("cod_program_cip_code")]
    public object? CodProgramCipCode { get; set; }

    [JsonPropertyName("cod_plus_application_id")]
    public object? CodPlusApplicationId { get; set; }

    [JsonPropertyName("cod_borrower_id")] public object? CodBorrowerId { get; set; }

    [JsonPropertyName("refunds_go_to")] public string? RefundsGoTo { get; set; }

    [JsonPropertyName("cod_most_recent_response_id")]
    public object? CodMostRecentResponseId { get; set; }

    [JsonPropertyName("cod_status")] public object? CodStatus { get; set; }

    [JsonPropertyName("cod_sync_logic")] public string? CodSyncLogic { get; set; }

    [JsonPropertyName("cod_syncable")] public bool? CodSyncable { get; set; }

    [JsonPropertyName("cod_syncable_errors")]
    public object? CodSyncableErrors { get; set; }

    [JsonPropertyName("cod_needs_sync")] public bool? CodNeedsSync { get; set; }

    [JsonPropertyName("cod_originated")] public bool? CodOriginated { get; set; }

    [JsonPropertyName("external_id")] public object? ExternalId { get; set; }

    [JsonPropertyName("added_at")] public DateTime? AddedAt { get; set; }

    [JsonPropertyName("added_by_id")] public int? AddedById { get; set; }

    [JsonPropertyName("esign_by_student_at")]
    public object? EsignByStudentAt { get; set; }

    [JsonPropertyName("esign_by_student_ip")]
    public object? EsignByStudentIp { get; set; }

    [JsonPropertyName("work_study_hours_per_week")]
    public object? WorkStudyHoursPerWeek { get; set; }*/

    [JsonPropertyName("report_data")] public ReportData? ReportData { get; set; }
}

public class ReportData
{
    [JsonPropertyName("aid_name")] public string? AidName { get; set; }

    [JsonPropertyName("aid_abbrv")] public string? AidAbbrv { get; set; }

    [JsonPropertyName("financial_aid_type")]
    public string? FinancialAidType { get; set; }

    [JsonPropertyName("is_scholarship")] public int? IsScholarship { get; set; }

    [JsonPropertyName("aid_source")] public string? AidSource { get; set; }

    [JsonPropertyName("firstname")] public string? Firstname { get; set; }

    [JsonPropertyName("lastname")] public string? Lastname { get; set; }

    [JsonPropertyName("amount_scheduled")] public string? AmountScheduled { get; set; }

    [JsonPropertyName("amount_disbursed")] public string? AmountDisbursed { get; set; }
}