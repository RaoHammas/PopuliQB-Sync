using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopDegree
{
    [JsonPropertyName("object")] public string? Object { get; set; }

    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("program_id")] public int? ProgramId { get; set; }

    [JsonPropertyName("department_id")] public int? DepartmentId { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("abbrv")] public string? Abbrv { get; set; }

    [JsonPropertyName("diploma")] public int? Diploma { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("cip_code")] public string? CipCode { get; set; }

    [JsonPropertyName("degree_level_id")] public int? DegreeLevelId { get; set; }

    [JsonPropertyName("unit")] public string? Unit { get; set; }

    [JsonPropertyName("distance_education")]
    public bool? DistanceEducation { get; set; }

    [JsonPropertyName("length")] public int? Length { get; set; }

    [JsonPropertyName("length_unit")] public string? LengthUnit { get; set; }

    [JsonPropertyName("external_id")] public string? ExternalId { get; set; }

    [JsonPropertyName("report_data")] public DegreeReportData? ReportData { get; set; }
}

public class DegreeReportData
{
    [JsonPropertyName("department_name")] public string? DepartmentName { get; set; }

    [JsonPropertyName("program_name")] public string? ProgramName { get; set; }

    [JsonPropertyName("formatted_cip_code")]
    public string? FormattedCipCode { get; set; }

    [JsonPropertyName("degree_level_name")]
    public string? DegreeLevelName { get; set; }
}