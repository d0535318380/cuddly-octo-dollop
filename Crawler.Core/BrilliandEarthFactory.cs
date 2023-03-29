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
        var url = UriFromString(sourceUrl);
        var attemps = 0;
        RingSummary[] instance; 


        do
        {
            var web = new HtmlWeb();
            var rootDocument = web.Load(url);
            var byMetal = CreateByMetal(url, rootDocument);
            var byCarat = CreateByCarat(url, rootDocument);
            instance = byMetal.Union(byCarat).ToArray();

            if (instance.Length == 0)
            {
                attemps++;
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        } while (instance.Length == 0 && attemps < 5);
       
        
        foreach (var item in instance)
        {
            Parse(item);
        }
        
        return Task.FromResult(instance);
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
        try
        {
            var videoScript = doc.QuerySelector("#model_video script");

            if (videoScript is null)
            {
                return;
            }

            var url = UriFromString(videoScript.GetAttributeValue("src", string.Empty));
            var video = new ContentItem()
            {
                Type = ContentType.Jsonp,
                Uri = url
            };
            
            item.VisualContentItems.Add(video);
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