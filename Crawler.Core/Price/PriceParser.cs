using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Crawler.Core;

public class PriceParser
{
    private static readonly Regex RowRegex = new(
        "data-(?<name>min|max|value|color|clear)=\"(?<value>[A-Z.\\d]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);


    public static async ValueTask<IEnumerable<DiamondPriceItem>> ReadAsync(string type, string fileName)
    {
        var content = await File.ReadAllTextAsync(fileName);
        var items = PriceParser.Parse(type, content);

        return items;
    }
        
    public static async Task WriteAsync(IEnumerable<DiamondPriceItem> items, string fileName)
    {
        await using (var writer = new StreamWriter(fileName))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(items);
        }
        
        
    }

    
    public static IEnumerable<DiamondPriceItem> Parse(string type, string source)
    {
        var doc = new HtmlDocument();

        doc.LoadHtml(source);

        var nodes = doc.DocumentNode.QuerySelectorAll("td[data-min]");

        foreach (var node in nodes)
        {
            var matches = RowRegex.Matches(node.OuterHtml);
            if (matches.Count < 5)
            {
                continue;
            }
            
            yield return DiamondPriceItem.Create(type, matches);
        }
    }
}