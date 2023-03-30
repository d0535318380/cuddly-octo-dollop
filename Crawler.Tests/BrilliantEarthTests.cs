using System.Text;
using System.Text.RegularExpressions;
using Crawler.Core;
using Downloader;
using FluentAssertions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit.Abstractions;

namespace Crawler.Tests;

public class BrilliantEarthTests
{
    [Fact]
    public async Task GetItemsTest()
    {
        var url = "https://www.brilliantearth.com/Gala-Diamond-Ring-White-Gold-BE1D6362P-9634461/";
        var factory = new BrilliantEarthFactory();
        var items = await factory.GetItemsAsync(url);

        var ringSummaries = items as RingSummary[] ?? items.ToArray();
        await ImageDownloader.DownloadAsync(ringSummaries);
        
        ringSummaries
            .Should()
            .NotBeEmpty();
    }


    [Fact]
    public async Task WebDriverTest()
    {
        WebDriver driver = new ChromeDriver();
        
        driver
            .Navigate()
            .GoToUrl("https://embed.imajize.com/3739689?v=1679993171");

        var source = driver.PageSource;
        
        Assert.NotNull(source);
    }
    
    [Fact]
    public async Task ParseItemsTest()
    {
        var items = await GetItems();
        var html = await File.ReadAllTextAsync(@"Data\BE1D6362P-9634461.html");
        var factory = new BrilliantEarthFactory();
        var item = factory.Parse(items.First());

        item
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task ParseJsonTest()
    {
        var expression = new Regex("\"(?<size>tn|md|lg)\"\\s*:\\s*[\"']//(?<link>[A-Z\\./0-9_]+)",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var html = await File.ReadAllTextAsync(@"Data\BE1D6362P-9634461.html");
        var doc = new HtmlDocument();
        var factory = new BrilliantEarthFactory();
        doc.LoadHtml(html);
        var images = factory.GetImages(html);
        var imGList = string.Join("\r\n", images
            .Select(x => x.Uri.AbsoluteUri)
            .OrderBy(x=>x)
            .Distinct()
        );
        await File.WriteAllTextAsync(@"Data\links.txt", imGList);
        
        return;
        
        var node = doc.QuerySelector("#ir309-explanation");
        var scripts = doc
            .QuerySelectorAll("script")
            .Where(x=>x.InnerText.Contains("image.brilliantearth.com"))
            .First(x => x.InnerText.Contains("product_shape_images"))
            .InnerText;

        
        
        var start = scripts.IndexOf("product_shape_images['Asscher'] = {", StringComparison.Ordinal);
        var text = scripts[start..]
            .Replace("product_shape_images['Asscher'] = ", string.Empty);
        var end = text.IndexOf("};", StringComparison.Ordinal) + 1;
        var jsonString = text[..end];
    }
    private async Task<IEnumerable<RingSummary>> GetItems()
    {
        var url = "https://www.brilliantearth.com/Gala-Diamond-Ring-White-Gold-BE1D6362P-9634461/";
        var html = await File.ReadAllTextAsync(@"Data\BE1D6362P-9634461.html");
        var factory = new BrilliantEarthFactory();
        var items = await factory.GetItemsAsync(url);

        return items;
    }
}