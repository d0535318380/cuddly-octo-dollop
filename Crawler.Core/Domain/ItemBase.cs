﻿using System.Text.Json.Serialization;

namespace Crawler.Core;

public class ItemBase
{
    public string Id { get; set; }
    public ContentType Type { get; set; }
    public Uri Uri { get; set; }
    public string Title { get; set; }
    
    [JsonIgnore]
    public string HtmlSource { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}