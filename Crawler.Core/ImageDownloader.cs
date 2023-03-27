using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Downloader;

namespace Crawler.Core;

public class DownloadItem
{
    public string Url { get; set; }
    public string BaseFolder { get; set; }

    public RingSummary RingSummary { get; set; }
}


public class ImageDownloader
{
    private static readonly Regex ImageLinksRegex = new Regex("('|\")(?<link>(//|https://)image[.]brilliantearth[.]com[A-Z0-9\\.\\-/_]+)('|\")", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        
    private static readonly Regex SizeRegex = new Regex("(?<size>\\d+)[_]?(ct|carat)", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    
    private static readonly Regex ShapeRegex = new Regex($"[_](?<shape>{string.Join("|", BrilliantEarthFactory.DiamondShapesMap.Keys)})[_]", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public static async Task DownloadAsync(RingSummary item, string path = "C:\\_Downloads", CancellationToken token = default)
    {
        var html = item.HtmlSource; 
        var baseFolder = Path.Combine(path, item.Upc);
        var matches = ImageLinksRegex.Matches(html)
            .Select(x => x.Groups["link"].Value)
            .Select(x=> x.StartsWith("//") ? x.Replace("//", "https://") : x)
            .Distinct()
            .Where(x => SizeRegex.IsMatch(x))
            .OrderBy(x => x)
            .Select(x=> new DownloadItem
            {
                Url = x,
                BaseFolder = baseFolder,
                RingSummary = item
            })
            .ToArray();

        
        
        var options = new ParallelOptions() { MaxDegreeOfParallelism  = 20 };
        await Parallel.ForEachAsync(matches, options, DownloadImage );
    }

    public static async Task DownloadAsync(IEnumerable<RingSummary> item, string path = "C:\\_Downloads",
        CancellationToken token = default)
    {
        var tasks = item.Select(x => DownloadAsync(x, path, token)).ToArray();

        await Task.WhenAll(tasks);
    }
    
    private static async ValueTask DownloadImage(DownloadItem item, CancellationToken token)
    {
            var downloader = new DownloadService();
            var shapeMatch = ShapeRegex.Match(item.Url);
            var sizeMatch = SizeRegex.Match(item.Url);
            var size = sizeMatch.Groups["size"].Value;
            var shape = shapeMatch.Groups["shape"].Value;

            if (size.Length == 1)
            {
                size += "00";
            }
            
            shape = BrilliantEarthFactory.DiamondShapesMap[shape];

            var target = Path.Combine(item.BaseFolder, shape, size);

            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            target = Path.Combine(target, Path.GetFileName(item.Url));
            
            await downloader
                .DownloadFileTaskAsync(
                    item.Url, target, token);
    }
}