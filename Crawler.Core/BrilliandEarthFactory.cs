using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.UI;

namespace Crawler.Core;

public partial class BrilliantEarthFactory : IRingSummaryFactory
{
    private static readonly Uri? _baseUrl = new Uri("https://brilliantearth.com");

    //var setting_video_url = '//embed.imajize.com/3739689?v=1679993171';
    private static readonly Regex View3dRegex = new(
        @"product_video_dict[[]'(?<shape>[A-Z]+)'[]]\s*=\s*'(?<code>\d+)'",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private WebDriver driver; 
    private HtmlDocument TryLoadHtml(Uri url)
    {
      //  using WebDriver driver = new ChromeDriver();

        driver
            .Navigate()
            .GoToUrl(url);

        Thread.Sleep(10);

        var rootDocument = new HtmlDocument();
        rootDocument.LoadHtml(driver.PageSource);

        return rootDocument;
    }

    public Task<RingSummary[]> GetItemsAsync(
        string sourceUrl, 
        CancellationToken token = default)
    {
        var url = UriFromString(sourceUrl);
        var attemps = 0;
        RingSummary[] instance;
        driver = new SafariDriver();
        do
        {
            var uri = new Uri(sourceUrl);
            var rootDocument = TryLoadHtml(uri);
            var byMetal = CreateByMetal(url, rootDocument);
            var byCarat = CreateByCarat(url, rootDocument);
            instance = byMetal.Union(byCarat).ToArray();

            if (instance.Length == 0)
            {
                attemps++;
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        } while (instance.Length == 0 && attemps < 5);


        foreach (var item in instance)
        {
            Parse(item);
        }

        driver.Dispose();
        driver = null;
        return Task.FromResult(instance);
    }

    public RingSummary Parse(RingSummary item)
    {
        var doc = TryLoadHtml(item.Uri);

        item.Title = doc.QuerySelector("h1").InnerText.Trim();
        item.Description = doc.QuerySelector("p.ir309-description").InnerText.Trim();

        GetVisualContentItems(item, doc);
        GetProperties(item, doc);

        item.HtmlSource = doc.DocumentNode.InnerHtml;

        Console.WriteLine($"{item.Upc} : {item.Title}");

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
        var videoScript = doc.QuerySelector("#model_video script");
        var productCodes = View3dRegex.Matches(doc.Text);

        try
        {
            if (videoScript is not null)
            {
                var url = UriFromString(videoScript.GetAttributeValue("src", string.Empty));
                var video = new ContentItem()
                {
                    Type = ContentType.Jsonp,
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
            // var clientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            // var client = new HttpClient(clientHandler);
            //
            // var response = await client.PostAsync(BaseUri, new FormUrlEncodedContent(parameters));
            // var contents = await response.Content.ReadAsStringAsync();
            //
            // return contents;
            HtmlWeb.PreRequestHandler handler = delegate(HttpWebRequest request)
            {
                request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.CookieContainer = new System.Net.CookieContainer();
                return true;
            };

            foreach (Match match in productCodes)
            {
                var webClient = new HtmlWeb();
                webClient.PreRequest += handler;
                var url = new Uri(
                    $"https://embed.imajize.com/{match.Groups["code"].Value}?v={DateTime.UnixEpoch.Microsecond}");
                var html = webClient.Load(url); // TryLoadHtml(url);
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

    private static IEnumerable<RingSummary> CreateByMetal(Uri url, HtmlDocument htmlDoc)
    {
        var rootNode = htmlDoc.QuerySelector("ul.pdp-metals-select-redesign");

        if (rootNode is null)
        {
            Console.WriteLine($"Metals not founds for {url.AbsolutePath}");
            yield break;
        }

        var metals = htmlDoc
            .QuerySelector("ul.pdp-metals-select-redesign")
            .QuerySelectorAll("a");

        foreach (var node in metals)
        {
            var upc = node.Attributes["data-upc"].Value;
            var parts = upc.Split("-");
            var instance = new RingSummary()
            {
                Id = node.GetAttributeValue("data-id", string.Empty),
                Sku = parts.FirstOrDefault(),
                Upc = upc,
                MetalName = node.GetAttributeValue("data-metal", string.Empty),
                MetalCode = parts.LastOrDefault(),
                Uri = UriFromString(node.GetAttributeValue("href", string.Empty))
            };
            yield return instance;
        }
    }

    private static IEnumerable<RingSummary> CreateByCarat(Uri uri, HtmlDocument htmlDoc)
    {
        try
        {
            return CreateByCaratInternal(uri, htmlDoc);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static IEnumerable<RingSummary> CreateByCaratInternal(Uri uri, HtmlDocument htmlDoc)
    {
        var rootNode = htmlDoc.QuerySelector("ul.ir309-carats-select");

        if (rootNode == null)
        {
            Console.WriteLine($"Carats not founds for {uri.AbsolutePath}");

            yield break;
        }


        var carats = rootNode.QuerySelectorAll("a")
            .Where(x => !x.HasClass("active"));

        foreach (var node in carats)
        {
            var url = node.GetAttributeValue("href", string.Empty);

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var web = new HtmlWeb();
            var rootDocument = web.Load(UriFromString(url));

            var items = CreateByMetal(uri, rootDocument);

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    private static Uri UriFromString(string uri, Uri? baseUrl = default)
    {
        if (uri.StartsWith("//"))
        {
            uri = $"https:{uri}";
        }

        return uri.StartsWith("/")
            ? new Uri(baseUrl ?? _baseUrl, uri)
            : new Uri(uri);
    }
}