namespace Crawler.Core;

public interface IRingSummaryFactory
{
    Task<RingSummary[]>  GetItemsAsync(
        string sourceUrl, 
        ImageDownloaderConfig? config = default, 
        CancellationToken token = default);
}