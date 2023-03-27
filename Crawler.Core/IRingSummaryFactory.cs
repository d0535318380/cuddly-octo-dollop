namespace Crawler.Core;

public interface IRingSummaryFactory
{
    Task<RingSummary[]>  GetItemsAsync(string sourceUrl, CancellationToken token = default);
    RingSummary Parse(RingSummary item);
}