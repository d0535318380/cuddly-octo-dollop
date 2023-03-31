using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Sources;
using Downloader;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Crawler.Core;

public class ImageDownloaderConfig
{
    public bool DownloadImages { get; set; } = true;
    public bool DownloadVideos { get; set; } = true;
    public bool DownloadView3d { get; set; } = true;
    public bool SkipExisted { get; set; } = true;
    public string Path { get; set; } = "Output";
}

public class DownloadItem
{
    public string Url { get; set; }
    public string BaseFolder { get; set; }
    public RingSummary RingSummary { get; set; }
    public string? FileName { get; set; }
    public bool IsImage { get; set; } = false;

    private ImageDownloaderConfig Config { get; set; }
}

public class ImageDownloader
{
    private static readonly Regex ProductRegex = new Regex(
        "BE[A-Z0-9]+[-_]",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex ImageLinksRegex = new Regex(
        "('|\")(?<link>(//|https://)(image|css)[.]brilliantearth[.]com[A-Z0-9\\.\\-/_]+)('|\")",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex VideoLinksRegex = new Regex(
        "('|\")(?<link>(//|https://)[A-Z0-9\\.\\-/_]+)('|\")",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex SizeRegex1 = new Regex("(?<size>\\d+)[_]?(ct|carat)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex ShapeRegex = new Regex(
        $"[_](?<shape>{string.Join("|", BrilliantEarthFactory.DiamondShapesMap.Keys)})[_]?",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex SizeRegex2 = new Regex(
        $"(?<shape>{string.Join("|", BrilliantEarthFactory.DiamondShapesMap.Keys)})(?<size>\\d+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static async Task DownloadInternalAsync(
        RingSummary item,
        ImageDownloaderConfig? config = default,
        CancellationToken token = default)
    {
        config ??= new ImageDownloaderConfig();

        var path = config.Path;
        var stopWatch = new Stopwatch();
        var baseFolder = Path.Combine(path, item.Upc);
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        var downloadItems = Array.Empty<DownloadItem>();

        if (config.DownloadImages)
        {
            downloadItems = GetImages(item, baseFolder);
        }

        if (config.DownloadVideos)
        {
            var videos = await GetVideos(item, baseFolder);
            downloadItems = downloadItems.Union(videos).ToArray();
        }

        if (config.DownloadView3d)
        {
            var view3ds = await GetView3d(item, baseFolder);
            downloadItems = downloadItems.Union(view3ds).ToArray();
        }

        stopWatch.Start();

        Console.WriteLine($"{item.Upc}, Items: {downloadItems.Length} start");

        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        await File.WriteAllTextAsync(
            Path.Combine(baseFolder, $"{item.Upc}.json"), json, token);

        var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await File.AppendAllLinesAsync(
            Path.Combine(path, $"log-{timeStamp}.txt"),
            new[] { $"{item.Upc} : {item.Title} : {item.Uri}" }, token);

        var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
        await Parallel.ForEachAsync(
            downloadItems, options, DownloadImage);

        stopWatch.Stop();
        Console.WriteLine($"{item.Upc}: {stopWatch.Elapsed:g} Items: {downloadItems.Length} end");
    }

    private static DownloadItem[] GetImages(RingSummary item, string baseFolder)
    {
        var html = item.HtmlSource;
        var links = ImageLinksRegex.Matches(html)
            .Select(x => x.Groups["link"].Value)
            .Select(x => x.StartsWith("//") ? x.Replace("//", "https://") : x)
            .Distinct();
        var imageItems = links
            .Where(x => ProductRegex.IsMatch(x))
            .OrderBy(x => x)
            .Select(x => new DownloadItem
            {
                Url = x,
                BaseFolder = baseFolder,
                RingSummary = item,
                IsImage = true
            })
            .ToArray();
        return imageItems;
    }

    private static async Task<DownloadItem[]> GetVideos(RingSummary item, string baseFolder)
    {
        var items = new List<DownloadItem>();
        foreach (var contentItem in item.VisualContentItems.Where(x => x.Type == ContentType.Video))
        {
            try
            {
                var videos = await GetVideosInternalAsync(item, baseFolder, contentItem);

                items.AddRange(videos);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        return items.ToArray();
    }

    private static async Task<List<DownloadItem>> GetVideosInternalAsync(
        RingSummary item,
        string baseFolder,
        ContentItem contentItem)
    {
        var i = 1;
        var items = new List<DownloadItem>();
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
                    BaseFolder = Path.Combine(baseFolder, "__Videos__"),
                    RingSummary = item,
                    IsImage = false,
                    FileName = $"video-{i}.mp4"
                });

            i++;
        }

        return items;
    }

    private static async Task<DownloadItem[]> GetView3d(RingSummary item, string baseFolder)
    {
        var items = new List<DownloadItem>();
        foreach (var contentItem in item.VisualContentItems.Where(x => x.Type == ContentType.View3d))
        {
            try
            {
                var videos = await GetVView3dInternalAsync(item, baseFolder, contentItem);

                items.AddRange(videos);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        return items.ToArray();
    }

    private static async Task<List<DownloadItem>> GetVView3dInternalAsync(
        RingSummary item,
        string baseFolder,
        ContentItem contentItem)
    {
        var i = 1;
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(contentItem.HtmlSource);

        var nodes = htmlDoc.QuerySelectorAll("img");

        var items = nodes.Select(x =>
        {
            var json =
                Teplates.VideoPattern
                    .Replace("{0}", contentItem.Code)
                    .Replace("{1}", x.GetAttributeValue("data-source", string.Empty));

            json += "\r\n";
            
            var base64 =  Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            var url = $"https://reflyster.imajize.com/{base64}";
            
            return new DownloadItem()
            {
                Url = url,
                BaseFolder = Path.Combine(baseFolder, contentItem.Folder, "View3d"),
                RingSummary = item,
                IsImage = false,
                FileName = x.GetAttributeValue("data-source", string.Empty)
            };
        }).ToList();
            

        return items;
    }


    public static async Task DownloadAsync(
        IEnumerable<RingSummary> items,
        ImageDownloaderConfig? config = default,
        CancellationToken token = default)
    {
        foreach (var item in items)
        {
            try
            {
                await DownloadInternalAsync(item, config, token);
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

    private static async ValueTask DownloadItemInternal(
        DownloadItem item, 
        CancellationToken token)
    {
        Console.WriteLine($"{item.RingSummary.Upc} : {item.Url}");

        var downloader = new DownloadService();
        var target = GetDirectory(item);

        target = Path.Combine(target, item.FileName ?? Path.GetFileName(item.Url));

        if (File.Exists(target))
        {
            return;
        }
        
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
        var sizeMatch = SizeRegex1.IsMatch(item.Url) ? SizeRegex1.Match(item.Url) : SizeRegex2.Match(item.Url);
        var size = sizeMatch.Groups["size"].Value;
        var shape = shapeMatch.Groups["shape"].Value;

        if (size.Length == 1)
        {
            size += "00";
        }

        if (!sizeMatch.Success)
        {
            size = "";
        }

        if (!BrilliantEarthFactory.DiamondShapesMap.ContainsKey(shape))
        {
            Console.WriteLine($"Shape not found: [{shape}] in {item.Url}");
            shape = "__Images__";
        }
        else
        {
            shape = BrilliantEarthFactory.DiamondShapesMap[shape];
        }

        if (!shapeMatch.Success)
        {
            size = string.Empty;
        }

        var target = Path.Combine(item.BaseFolder, shape, size);

        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
        }

        return target;
    }
}