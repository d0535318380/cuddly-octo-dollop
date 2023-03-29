using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Sources;
using Downloader;

namespace Crawler.Core;

public class DownloadItem
{
    public string Url { get; set; }
    public string BaseFolder { get; set; }
    public RingSummary RingSummary { get; set; }
    public string? FileName { get; set; }

    public bool IsImage { get; set; } = false;
}

public class ImageDownloader
{
    private static readonly Regex ImageLinksRegex = new Regex(
        "('|\")(?<link>(//|https://)image[.]brilliantearth[.]com[A-Z0-9\\.\\-/_]+)('|\")",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex VideoLinksRegex = new Regex(
        "('|\")(?<link>(//|https://)[A-Z0-9\\.\\-/_]+)('|\")",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex SizeRegex = new Regex("(?<size>\\d+)[_]?(ct|carat)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex ShapeRegex = new Regex(
        $"[_](?<shape>{string.Join("|", BrilliantEarthFactory.DiamondShapesMap.Keys)})[_]",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static async Task DownloadInternalAsync(RingSummary item, string path = "C:\\_Downloads",
        CancellationToken token = default)
    {
        var stopWatch = new Stopwatch();
        var html = item.HtmlSource;
        var baseFolder = Path.Combine(path, item.Upc);
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
        var imageItems = ImageLinksRegex.Matches(html)
            .Select(x => x.Groups["link"].Value)
            .Select(x => x.StartsWith("//") ? x.Replace("//", "https://") : x)
            .Distinct()
            .Where(x => SizeRegex.IsMatch(x))
            .OrderBy(x => x)
            .Select(x => new DownloadItem
            {
                Url = x,
                BaseFolder = baseFolder,
                RingSummary = item,
                IsImage = true
            })
            .ToArray();
        var videos = await GetVideos(item, baseFolder);

        imageItems = imageItems.Union(videos).ToArray();

        stopWatch.Start();

        Console.WriteLine($"{item.Upc}, Items: {imageItems.Length} start");
        
        
        
        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        await File.WriteAllTextAsync(
            Path.Combine(baseFolder, $"{item.Upc}.json"), json, token);

        var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await File.AppendAllLinesAsync(
            Path.Combine(path, $"log-{timeStamp}.txt"), 
            new [] { $"{item.Upc} : {item.Title} : {item.Uri}" });
        
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
        await Parallel.ForEachAsync(
            imageItems, options, DownloadImage);

        stopWatch.Stop();
        Console.WriteLine($"{item.Upc}: {stopWatch.Elapsed:g} Items: {imageItems.Length} end");
    }

    private static async Task<DownloadItem[]> GetVideos(RingSummary item, string baseFolder)
    {
        var items = new List<DownloadItem>();

        var i = 1;
        foreach (var contentItem in item.VisualContentItems)
        {
            var web = new HttpClient();
            var result = await web.GetStringAsync(contentItem.Uri);
            var matches = VideoLinksRegex.Matches(result);

            foreach (Match match in matches)
            {
                var link = match.Groups["link"].Value;

                if (!link.EndsWith("bin", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                items.Add(
                    new DownloadItem()
                    {
                        Url = link,
                        BaseFolder = baseFolder,
                        RingSummary = item,
                        IsImage = false,
                        FileName = $"video-{i}.mp4"
                    });

                i++;
            }
        }

        return items.ToArray();
    }

    public static async Task DownloadAsync(IEnumerable<RingSummary> items, string path = @".\Output",
        CancellationToken token = default)
    {
        foreach (var item in items)
        {
            try
            {
                await DownloadInternalAsync(item, path, token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{item.Upc} : Download failed = {e.Message}");
                Console.WriteLine(e);
            }
        }
    }

    private static async ValueTask DownloadImage(DownloadItem item, CancellationToken token)
    {
        try
        {
            await DownloadItemInternal(item, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static async ValueTask DownloadItemInternal(DownloadItem item, CancellationToken token)
    {
        Console.WriteLine($"{item.RingSummary.Upc} : {item.Url}");

        var downloader = new DownloadService();
        var target = GetDirectory(item);

        target = Path.Combine(target, item.FileName ?? Path.GetFileName(item.Url));
        
        await downloader
            .DownloadFileTaskAsync(
                item.Url, target, token);
    }

    private static string GetDirectory(DownloadItem item)
    {
        if (!item.IsImage)
        {
            return item.BaseFolder;
        }

        var shapeMatch = ShapeRegex.Match(item.Url);
        var sizeMatch = SizeRegex.Match(item.Url);
        var size = sizeMatch.Groups["size"].Value;
        var shape = shapeMatch.Groups["shape"].Value;

        if (size.Length == 1)
        {
            size += "00";
        }

        if (!BrilliantEarthFactory.DiamondShapesMap.ContainsKey(shape))
        {
            Console.WriteLine($"Shape not found: [{shape}] in {item.Url}");
            shape = "Unknown";
        }
        else
        {
            shape = BrilliantEarthFactory.DiamondShapesMap[shape];
        }

        var target = Path.Combine(item.BaseFolder, shape, size);

        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
        }

        return target;
    }
}