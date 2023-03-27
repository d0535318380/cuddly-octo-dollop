namespace Crawler.Core;

public class ItemSummary: ItemBase
{
    public string Upc { get; set; }
    public string Sku { get; set; }
    public ICollection<ContentItem> VisualContentItems { get; set; } = new List<ContentItem>();
}