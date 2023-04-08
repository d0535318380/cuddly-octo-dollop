using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Crawler.Core;

public class ParserContext
{
    public Uri RootUri { get; set; }

    public ConcurrentDictionary<Uri, RingSummary> Items = new();
}


public partial class BrilliantEarthFactory : IRingSummaryFactory
{
    private static readonly Uri? BaseUrl = new("https://brilliantearth.com");
    private WebDriver driver;
    
    #region Regex

    public static Regex ProductRegex = new (
        @"[""']sku[""']\s+[:]\s+[""'](?<code>BE[A-Z0-9]+)[-](?<metal>[0-9A-Z]{2,4})", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    
    //var setting_video_url = '//embed.imajize.com/3739689?v=1679993171';
    private static readonly Regex View3dRegex = new(
        @"product_video_dict[[]'(?<shape>[A-Z]+)'[]]\s*=\s*'(?<code>\d+)'",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static Regex VideoRegex = new(
        "(?<link>(//|https://)fast[.]wistia[.]com/embed/medias/[A-Z0-9]+[.]jsonp)", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    
    private static Regex View3dSourceRegex = new(
        @"var\s+setting_video_url\s*=\s*['](?<link>(//|https://)embed[.]imajize[.]com/(?<code>\d+)[?]v=\d+)[']\s*;", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    
    private static Regex WhiteSpaceRegex = new(
        @"[\s\r\n]+", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    
    private static Regex CategoryRegex = new(
        @"[""']og:category[""']\s+content=[""'](?<category>[A-Z0-9\s\-]+)[""']", 
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    #endregion
     
    private HtmlDocument TryLoadHtml(Uri url)
    {
        driver
            .Navigate()
            .GoToUrl(url);

        Thread.Sleep(10);

        var rootDocument = new HtmlDocument();
        rootDocument.LoadHtml(driver.PageSource);

        return rootDocument;
    }

    public async Task<RingSummary[]> GetItemsAsync(
        string sourceUrl,
        ImageDownloaderConfig? config = default,
        CancellationToken token = default)
    {
        config ??= new ImageDownloaderConfig();
        
        var uri = UriFromString(sourceUrl);
        var context = new ParserContext()
        {
            RootUri = uri
        };        

        driver = new ChromeDriver();

        await LoadVariations(uri, context, config, token);

        driver.Dispose();
        driver = null;
        
        Console.WriteLine($"Total items: {context.Items.Count}");
        
        return context.Items.Values.ToArray();
    }

    private async Task LoadVariations(
        Uri uri, 
        ParserContext context, 
        ImageDownloaderConfig? config,
        CancellationToken token)
    {
        var map = context.Items;

        if (map.ContainsKey(uri) && !string.IsNullOrWhiteSpace(map[uri].HtmlSource))
        {
            return;
        }
        
        Console.WriteLine($"Parsing: {uri.AbsolutePath}");
        
        var item = CreateRingSummary(uri);
        var relatedItemLinks = GetRelatedItemLinks(item, context);
        
        map[item.Uri] = item;

        Console.WriteLine($"Parsed: {item.Upc}");
        await ContentHelper.WriteAsync(item, config, token);
        
        foreach (var link in relatedItemLinks)
        {
            await LoadVariations(link, context, config, token);
        }
    }

    private static IEnumerable<Uri> GetRelatedItemLinks(RingSummary item, ParserContext context)
    {
        // ir229-pdp-metals-select ir309-carats-select
        // ir309-carats-select
        // 
        var doc = new HtmlDocument();
        doc.LoadHtml(item.HtmlSource);
        
        var caratsItems = doc.QuerySelectorAll("ul.ir309-carats-select") ?? new List<HtmlNode>();
        var metalItems = doc.QuerySelectorAll("ul.pdp-metals-select-redesign") ?? new List<HtmlNode>();

        Uri[] links = metalItems
            .Union(caratsItems)
            .QuerySelectorAll("a")
            .Select(x => x.GetAttributeValue("href", string.Empty))
            .Select(x => UriFromString(x))
            .Where(x => x is not null)
            .Where(x => !context.Items.ContainsKey(x))
            .ToArray();

        return links;
    }

    private RingSummary CreateRingSummary(Uri uri)
    {
        var doc = TryLoadHtml(uri);
        var match = ProductRegex.Match(doc.Text);
        var category = CategoryRegex.Match(doc.Text);
        var item = new RingSummary
        {
            Uri = uri,
            Sku = match.Groups["code"].Value,
            MetalCode = match.Groups["metal"].Value,
            MetalName = match.Groups["metal"].Value,
            HtmlSource = doc.Text,
            Title = Normalize(doc.QuerySelector("h1")),
            Description = Normalize(doc.QuerySelector("p.ir309-description")),
            Category = Normalize(category.Groups["category"].Value)
        };

        item.Upc = $"{item.Sku}-{item.MetalCode}";
        
        GetVisualContentItems(item, doc);
        GetProperties(item, doc);
        
        return item;
    }


    private static void GetProperties(ItemSummary summary, HtmlDocument doc)
    {
        var items = doc.QuerySelectorAll("#JS-Diamond-details dl");

        foreach (var node in items)
        {
            var key = node.QuerySelector("dt")
                .LastChild.InnerText
                .Trim()
                .Replace(":", string.Empty);

            var val = node.QuerySelector("dd").InnerText.Trim();
            summary.Properties[key] = val;
        }
    }

    private static void GetVisualContentItems(RingSummary item, HtmlDocument doc)
    {
        var videoScript = VideoRegex.Matches(doc.Text);
        var productCodes = View3dRegex.Matches(doc.Text);
        HtmlWeb.PreRequestHandler handler = delegate(HttpWebRequest request)
        {
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.CookieContainer = new CookieContainer();
            return true;
        };

        try
        {
            foreach (Match match in videoScript)
            {
                var url = UriFromString(match.Groups["link"].Value);
                var video = new ContentItem()
                {
                    Type = ContentType.Video,
                    Uri = url
                };

                item.VisualContentItems.Add(video);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }


        try
        {

            var sourceUrl = View3dSourceRegex.Match(doc.Text);

            if (sourceUrl.Success)
            {
                var webClient = new HtmlWeb();
                webClient.PreRequest += handler;
                var url = UriFromString(sourceUrl.Groups["link"].Value);
                var html = webClient.Load(url); 
                var video = new ContentItem
                {
                    Code = sourceUrl.Groups["code"].Value,
                    Folder = string.Empty,
                    Type = ContentType.View3d,
                    HtmlSource = html.Text,
                    Uri = url,
                };

                item.VisualContentItems.Add(video);
            }
            
            
            foreach (Match match in productCodes)
            {
                var webClient = new HtmlWeb();
                webClient.PreRequest += handler;
                var url = new Uri(
                    $"https://embed.imajize.com/{match.Groups["code"].Value}?v={DateTime.UnixEpoch.Microsecond}");
                var html = webClient.Load(url); 
                var video = new ContentItem
                {
                    Code = match.Groups["code"].Value,
                    Folder = match.Groups["shape"].Value,
                    Type = ContentType.View3d,
                    HtmlSource = html.Text,
                    Uri = url,
                };

                item.VisualContentItems.Add(video);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static Uri? UriFromString(string uri, Uri? baseUrl = default)
    {
        try
        {
            if (uri.StartsWith("//"))
            {
                uri = $"https:{uri}";
            }

            return uri.StartsWith("/")
                ? new Uri(baseUrl ?? BaseUrl, uri)
                : new Uri(uri);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed parse URI: {uri}");
            Console.WriteLine(e);
            return null;
        }
        
    }

    public static string Normalize(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }


        return
            WhiteSpaceRegex
                .Replace(source, " ")
                .Trim();
    }
    public static string Normalize(HtmlNode source)
    {
        return
            WhiteSpaceRegex
                .Replace(source?.InnerText ?? string.Empty, " ")
                .Trim();
    }
}