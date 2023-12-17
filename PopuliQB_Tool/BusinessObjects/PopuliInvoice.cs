using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class Invoice
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("actor_type")] public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")] public int? ActorId { get; set; }

    [JsonPropertyName("number")] public int? Number { get; set; }

    [JsonPropertyName("description")] public object? Description { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("due_on")] public string? DueOn { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("marked_uncollectible_on")]
    public object? MarkedUncollectibleOn { get; set; }

    [JsonPropertyName("report_data")] public InvoiceReportData? ReportData { get; set; }

    [JsonPropertyName("posted_on")] public string? PostedOn { get; set; }

    [JsonPropertyName("academic_term_id")] public int? AcademicTermId { get; set; }

    [JsonPropertyName("items")] public List<InvoiceItem>? Items { get; set; }
}

public class InvoiceItem
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("invoice_id")] public int? InvoiceId { get; set; }

    [JsonPropertyName("item_type")] public string? ItemType { get; set; }

    [JsonPropertyName("item_id")] public int? ItemId { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("amount")] public int? Amount { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }
}

public class InvoiceReportData
{
    [JsonPropertyName("amount_paid")] public string? AmountPaid { get; set; }

    [JsonPropertyName("balance")] public string? Balance { get; set; }

    [JsonPropertyName("overdue")] public int? Overdue { get; set; }

    [JsonPropertyName("term_name")] public string? TermName { get; set; }

    [JsonPropertyName("term_start_date")] public string? TermStartDate { get; set; }

    [JsonPropertyName("firstname")] public string? Firstname { get; set; }

    [JsonPropertyName("lastname")] public string? Lastname { get; set; }

    [JsonPropertyName("preferred_name")] public string? PreferredName { get; set; }

    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }

    [JsonPropertyName("personid")] public int? PersonId { get; set; }

    [JsonPropertyName("studentid")] public int? StudentId { get; set; }

    [JsonPropertyName("posted_date")] public string? PostedDate { get; set; }

    [JsonPropertyName("dummyid")] public string? DummyId { get; set; }

    [JsonPropertyName("person_amount_paid")]
    public object? PersonAmountPaid { get; set; }

    [JsonPropertyName("org_amount_paid")] public object? OrgAmountPaid { get; set; }

    [JsonPropertyName("aid_amount_paid")] public object? AidAmountPaid { get; set; }

    [JsonPropertyName("plan_name")] public object? PlanName { get; set; }

    [JsonPropertyName("scheduled_aid_handling")]
    public object? ScheduledAidHandling { get; set; }

    [JsonPropertyName("recurring_money_transfer_linkable_term_level")]
    public object? RecurringMoneyTransferLinkableTermLevel { get; set; }

    [JsonPropertyName("recurring_money_transfer_linkable_existing")]
    public object? RecurringMoneyTransferLinkableExisting { get; set; }

    [JsonPropertyName("on_term_level_payment_plan")]
    public int? OnTermLevelPaymentPlan { get; set; }

    [JsonPropertyName("invoice_due_date")] public string? InvoiceDueDate { get; set; }

    [JsonPropertyName("plan_due_date")] public object? PlanDueDate { get; set; }

    [JsonPropertyName("on_payment_plan")] public int? OnPaymentPlan { get; set; }

    [JsonPropertyName("on_plan_total")] public object? OnPlanTotal { get; set; }
}