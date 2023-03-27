namespace Crawler.Core;

public class RingSummary : ItemSummary
{
    public string Shape { get; set; }
    public string MetalCode { get; set; }
    public string MetalName { get; set; }
    
    public DiamondSummary Diamond { get; set; } = new();
    public DiamondSummary DiamondMele { get; set; } = new();
}