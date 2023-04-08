using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium.Chrome;

namespace Crawler.Core;

public partial class BrilliantEarthFactory 
{
    [Obsolete]
    public Task<RingSummary[]> GetItemsObsoleteAsync(
        string sourceUrl, 
        CancellationToken token = default)
    {
        var url = UriFromString(sourceUrl);
        var attemps = 0;
        RingSummary[] instance;
        
        driver = new ChromeDriver();
        
        do
        {
            var uri = new Uri(sourceUrl);
            var rootDocument = TryLoadHtml(uri);
            var byMetal = CreateByMetalObsolete(url, rootDocument);
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
            ParseObsolete(item);
        }

        driver.Dispose();
        driver = null;
        return Task.FromResult(instance);
    }

    
    
    public RingSummary ParseObsolete(RingSummary item)
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
    
    [Obsolete]
    private static IEnumerable<RingSummary> CreateByMetalObsolete(Uri url, HtmlDocument htmlDoc)
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
        // ir229-pdp-metals-select ir309-carats-select
        // ir309-carats-select
        
        var rootNode = htmlDoc.QuerySelectorAll("ul.ir309-carats-select") ?? new List<HtmlNode>();
        
        if (!rootNode.Any())
        {
            Console.WriteLine($"Carats not founds for {uri.AbsolutePath}");

            yield break;
        }

        var carats = rootNode.QuerySelectorAll("a");

        foreach (var node in carats)
        {
            var url = node.GetAttributeValue("href", string.Empty);

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var web = new HtmlWeb();
            var rootDocument = web.Load(UriFromString(url));

            var items = CreateByMetalObsolete(uri, rootDocument);

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

}