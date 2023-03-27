namespace Crawler.Core;

public class DiamondSummary: ItemSummary
{
    public int Count { get; set; } = 1;
    public string Cut { get; set; }
    public string Carat { get; set; }
    public string Clarity { get; set; }
    public string Color { get; set; }
}