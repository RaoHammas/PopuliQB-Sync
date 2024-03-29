﻿using System.Text.Json.Serialization;

namespace PopuliQB_Tool.PopuliFilters;

public class PopFilterValueStatusId
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}
public class PopFilterValueDisplayName
{
    [JsonPropertyName("display_text")]
    public string DisplayText { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class PopFilterValueText
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}
 
public class PopFilterValueDateRange
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("start")]
    public string Start { get; set; }
    
    [JsonPropertyName("end")]
    public string End { get; set; }
    // public string DaysAgo { get; set; }
}

public class PopFilterTypeField
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; } // Use object type to handle both Value and ValueRange

    [JsonPropertyName("positive")]
    public string Positive { get; set; }
}

public class PopFilterItem
{
    [JsonPropertyName("logic")]
    public string Logic { get; set; }

    [JsonPropertyName("fields")]
    public List<PopFilterTypeField> Fields { get; set; }
}

public class PopFilter
{
    [JsonPropertyName("expand")]
    public string[] Expand { get; set; }

    [JsonPropertyName("filter")]
    public List<PopFilterItem> FilterItems { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}