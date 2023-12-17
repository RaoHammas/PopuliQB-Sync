using System.Text.Json.Serialization;

namespace PopuliQB_Tool.BusinessObjects;

public class PopuliResponse<T>
{
    
    [JsonPropertyName("object?")]
    public string? Object { get; set; }

    
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    
    [JsonPropertyName("results")]
    public int? Results { get; set; }

    
    [JsonPropertyName("results_per_page")]
    public int? ResultsPerPage { get; set; }

    
    [JsonPropertyName("pages")]
    public int? Pages { get; set; }

    
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }


    
    [JsonPropertyName("data")]
    public List<T> Data { get; set; }
}