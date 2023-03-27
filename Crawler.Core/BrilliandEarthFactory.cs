using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Crawler.Core;



public partial class BrilliantEarthFactory : IRingSummaryFactory
{
    private static readonly Uri? _baseUrl = new Uri("https://brilliantearth.com");
    private static readonly Regex _keyValuRegex = new(@"(?<key>[\w\s.,]+)\s+[:]\s+(?<value>.*)");

    public Task<RingSummary[]> GetItemsAsync(string sourceUrl,  CancellationToken token = default)
    {
        var web = new HtmlWeb();
        var rootDocument = web.Load(sourceUrl);
        var instance = CreateInstance(rootDocument).ToArray();

        
        foreach (var item in instance)
        {
            Parse(item);
        }
        
        
        return Task.FromResult(instance.ToArray());
    }

    public RingSummary Parse(RingSummary item)
    {
        var web = new HtmlWeb();
        var doc = web.Load(item.Uri);
        
        var node = doc.QuerySelector("#ir309-explanation");

        item.Title = node.QuerySelector("h1").InnerText.Trim();
        item.Description = node.QuerySelector("p.ir309-description").InnerText.Trim();

        GetVisualContentItems(item, doc);
        GetProperties(item, doc);

        item.HtmlSource = doc.DocumentNode.InnerHtml;
        
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
        item.VisualContentItems.Add(new ContentItem()
        {
            Type = ContentType.Jsonp,
            Uri = UriFromString(videoScript.GetAttributeValue("src", string.Empty))
        });
    }

    private static IEnumerable<RingSummary> CreateInstance(HtmlDocument htmlDoc)
    {
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