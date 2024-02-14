using RestSharp;
using System.Xml.Serialization;
using RestSharp.Serializers.Xml;

namespace PopuliQB_Tool.BusinessServices;

public class OldPopuliAccessService
{
    private readonly string? _url;
    private readonly RestClient _client;
    private string _accessKey = "";
    private string? _userName;
    private string? _userPass;

    public OldPopuliAccessService(AppConfiguration appConfiguration)
    {
        _url = appConfiguration["API_URL_OLD"];
        _userName = appConfiguration["USER_NAME"];
        _userPass = appConfiguration["USER_PASS"];
        _client = new RestClient(new RestClientOptions
        {
            ThrowOnDeserializationError = false,
            ThrowOnAnyError = false,
        });
    }

    public async Task<string> GetAccessToken()
    {
        var request = new RestRequest($"{_url}", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        // request.AddHeader("Accept", "application/xml");
        request.AddParameter("username", _userName);
        request.AddParameter("password", _userPass);
        request.AddParameter("account_type", "");
        var response = await _client.ExecuteAsync(request);
        if (response is { IsSuccessful: true, Content: not null })
        {
            var dotNetXmlDeserializer = new DotNetXmlDeserializer();
            var data = dotNetXmlDeserializer.Deserialize<OldLoginResponse>(response);
            if (data != null)
            {
                _accessKey = data.AccessKey!;
                return _accessKey;
            }
        }

        return "";
    }

    public async Task<OldInvoices?> GetSalesCreditsAsync(int studentId)
    {
        var request = new RestRequest($"{_url}", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("access_key", _accessKey);
        request.AddParameter("task", "getInvoices");
        request.AddParameter("student_id", $"{studentId}");
        request.AddParameter("type", "CREDIT");

        var response = await _client.ExecuteAsync(request);
        if (response is { IsSuccessful: true, Content: not null })
        {
            var dotNetXmlDeserializer = new DotNetXmlDeserializer();
            try
            {
                var data = dotNetXmlDeserializer.Deserialize<OldInvoiceResponse>(response);
                if (data != null)
                {
                    return data.Invoices;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        return new();
    }
}

[XmlRoot(ElementName = "response")]
public class OldLoginResponse
{
    [XmlElement(ElementName = "access_key")]
    public string? AccessKey { get; set; }

    [XmlElement(ElementName = "accountid")]
    public int? Accountid { get; set; }

    [XmlElement(ElementName = "accounttype")]
    public string? Accounttype { get; set; }
}

// using System.Xml.Serialization;
// XmlSerializer serializer = new XmlSerializer(typeof(Response));
// using (StringReader reader = new StringReader(xml))
// {
//    var test = (Response)serializer.Deserialize(reader);
// }

[XmlRoot(ElementName = "charge")]
public class OldCharge
{
    [XmlElement(ElementName = "item_type")]
    public string? ItemType { get; set; }

    [XmlElement(ElementName = "invoice_item_id")]
    public string? InvoiceItemId { get; set; }

    [XmlElement(ElementName = "description")]
    public string? Description { get; set; }

    [XmlElement(ElementName = "amount")] public string? Amount { get; set; }

    [XmlElement(ElementName = "item_name")]
    public string? ItemName { get; set; }

    [XmlElement(ElementName = "finaid_applies")]
    public string? FinaidApplies { get; set; }
}

[XmlRoot(ElementName = "charges")]
public class OldCharges
{
    [XmlElement(ElementName = "charge")] public List<OldCharge>? Charges { get; set; }
}

[XmlRoot(ElementName = "invoice")]
public class OldInvoice
{
    [XmlElement(ElementName = "id")] public string? Id { get; set; }

    [XmlElement(ElementName = "type")] public string? Type { get; set; }

    [XmlElement(ElementName = "status")] public string? Status { get; set; }

    [XmlElement(ElementName = "invoice_number")]
    public string? InvoiceNumber { get; set; }

    [XmlElement(ElementName = "description")]
    public string? Description { get; set; }

    [XmlElement(ElementName = "person_id")]
    public string? PersonId { get; set; }

    [XmlElement(ElementName = "firstname")]
    public string? Firstname { get; set; }

    [XmlElement(ElementName = "lastname")] public string? Lastname { get; set; }

    [XmlElement(ElementName = "preferred_name")]
    public string? PreferredName { get; set; }

    [XmlElement(ElementName = "middlename")]
    public string? Middlename { get; set; }

    [XmlElement(ElementName = "amount")] public string? Amount { get; set; }

    [XmlElement(ElementName = "due_date")] public string? DueDate { get; set; }

    [XmlElement(ElementName = "term_id")] public string? TermId { get; set; }

    [XmlElement(ElementName = "term_name")]
    public string? TermName { get; set; }

    [XmlElement(ElementName = "student_id")]
    public string? StudentId { get; set; }

    [XmlElement(ElementName = "transaction_id")]
    public string? TransactionId { get; set; }

    [XmlElement(ElementName = "transaction_number")]
    public string? TransactionNumber { get; set; }

    [XmlElement(ElementName = "posted_date")]
    public string? PostedDate { get; set; }

    [XmlElement(ElementName = "added_by")] public string? AddedBy { get; set; }

    [XmlElement(ElementName = "added_time")]
    public string? AddedTime { get; set; }

    [XmlElement(ElementName = "transaction_status")]
    public string? TransactionStatus { get; set; }

    [XmlElement(ElementName = "payment_plan_id")]
    public string? PaymentPlanId { get; set; }

    [XmlElement(ElementName = "payment_plan_applied_at")]
    public string? PaymentPlanAppliedAt { get; set; }

    [XmlElement(ElementName = "payment_plan_name")]
    public string? PaymentPlanName { get; set; }

    [XmlElement(ElementName = "total_fin_aid_charges")]
    public string? TotalFinAidCharges { get; set; }

    [XmlElement(ElementName = "total_non_fin_aid_charges")]
    public string? TotalNonFinAidCharges { get; set; }

    [XmlElement(ElementName = "charges")] public OldCharges? Charges { get; set; }

    [XmlElement(ElementName = "credit_refunds")]
    public string? CreditRefunds { get; set; }
}

[XmlRoot(ElementName = "invoices")]
public class OldInvoices
{
    [XmlElement(ElementName = "invoice")] public List<OldInvoice?> Invoices { get; set; }
}

[XmlRoot(ElementName = "response")]
public class OldInvoiceResponse
{
    [XmlElement(ElementName = "invoices")] public OldInvoices? Invoices { get; set; }

    /*
    [XmlAttribute(AttributeName = "num_results")]
    public object NumResults { get; set; }*/
}