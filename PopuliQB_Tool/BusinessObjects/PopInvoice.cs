using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopInvoice
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("actor_type")] public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")] public int? ActorId { get; set; }

    [JsonPropertyName("number")] public int? Number { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("due_on")] public DateTime? DueOn { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("marked_uncollectible_on")]
    public object? MarkedUncollectibleOn { get; set; }

    [JsonPropertyName("report_data")] public PopInvoiceReportData? ReportData { get; set; }

    [JsonPropertyName("posted_on")] public string? PostedOn { get; set; }

    [JsonPropertyName("academic_term_id")] public int? AcademicTermId { get; set; }

    [JsonPropertyName("items")] public List<PopInvoiceItem>? Items { get; set; }
    [JsonPropertyName("credits")] public List<PopCredit>? Credits { get; set; }
    [JsonPropertyName("payments")] public List<PopPayment>? Payments { get; set; }

}

public class PopInvoiceItem
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("invoice_id")] public int? InvoiceId { get; set; }

    [JsonPropertyName("item_type")] public string? ItemType { get; set; }

    [JsonPropertyName("item_id")] public int? ItemId { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }
}

public class PopInvoiceReportData
{
    [JsonPropertyName("amount_paid")] public double? AmountPaid { get; set; }

    [JsonPropertyName("balance")] public double? Balance { get; set; }

    [JsonPropertyName("overdue")] public object? Overdue { get; set; }

    [JsonPropertyName("term_name")] public string? TermName { get; set; }

    [JsonPropertyName("term_start_date")] public DateTime? TermStartDate { get; set; }

    [JsonPropertyName("firstname")] public string? Firstname { get; set; }

    [JsonPropertyName("lastname")] public string? Lastname { get; set; }

    [JsonPropertyName("preferred_name")] public string? PreferredName { get; set; }

    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }

    [JsonPropertyName("personid")] public int? PersonId { get; set; }

    [JsonPropertyName("studentid")] public int? StudentId { get; set; }

    [JsonPropertyName("posted_date")] public DateTime? PostedDate { get; set; }

    [JsonPropertyName("dummyid")] public string? DummyId { get; set; }

    [JsonPropertyName("person_amount_paid")]
    public double? PersonAmountPaid { get; set; }

    [JsonPropertyName("org_amount_paid")] public double? OrgAmountPaid { get; set; }

    [JsonPropertyName("aid_amount_paid")] public double? AidAmountPaid { get; set; }

    [JsonPropertyName("plan_name")] public object? PlanName { get; set; }

    [JsonPropertyName("scheduled_aid_handling")]
    public object? ScheduledAidHandling { get; set; }

    [JsonPropertyName("recurring_money_transfer_linkable_term_level")]
    public object? RecurringMoneyTransferLinkableTermLevel { get; set; }

    [JsonPropertyName("recurring_money_transfer_linkable_existing")]
    public object? RecurringMoneyTransferLinkableExisting { get; set; }

    [JsonPropertyName("on_term_level_payment_plan")]
    public object? OnTermLevelPaymentPlan { get; set; }

    [JsonPropertyName("invoice_due_date")] public DateTime? InvoiceDueDate { get; set; }

    [JsonPropertyName("plan_due_date")] public DateTime? PlanDueDate { get; set; }

    [JsonPropertyName("on_payment_plan")] public object? OnPaymentPlan { get; set; }

    [JsonPropertyName("on_plan_total")] public object? OnPlanTotal { get; set; }
}

public class PopCredit
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("actor_type")] public string? ActorType { get; set; }

    [JsonPropertyName("actor_id")] public int? ActorId { get; set; }

    [JsonPropertyName("number")] public int? Number { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("transaction_id")] public int? TransactionId { get; set; }

    [JsonPropertyName("amount")] public double? Amount { get; set; }

    [JsonPropertyName("due_on")] public DateTime? DueOn { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("posted_on")] public DateTime? PostedOn { get; set; }

    [JsonPropertyName("academic_term_id")] public object? AcademicTermId { get; set; }

    [JsonPropertyName("items")] public List<PopInvoiceItem>? Items { get; set; }
}
