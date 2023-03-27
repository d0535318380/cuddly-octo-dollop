namespace Crawler.Core;

public class ItemBase
{
    public string Id { get; set; }
    public string Type { get; set; }
    public Uri Uri { get; set; }
    public string Title { get; set; }
    public string HtmlSource { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}