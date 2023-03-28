using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Crawler.Core;

public partial class BrilliantEarthFactory
{
    public static readonly RegexOptions RegexOptions =
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase;

    public static readonly HashSet<string> DiamondShapes = new()
    {
        "Princess", "PR",
        "Asscher", "AS",
        "Cushion", "CU",
        "Emerald", "EM",
        "Heart", "HE",
        "Pear", "PE",
        "Marquise", "MQ",
        "Radiant", "RA",
        "Oval", "OV",
        "Round", "RD",
    };

    public static readonly Dictionary<string, string> DiamondShapesMap = new()
    {
        { "Princess", "Princess" },
        { "PR", "Princess" },
        { "Asscher", "Asscher" },
        { "AS", "Asscher" },
        { "Cushion", "Cushion" },
        { "CU", "Cushion" },
        { "Emerald", "Emerald" },
        { "EM", "Emerald" },
        { "Heart", "Heart" },
        { "HT", "Heart" },
        { "Pear", "Pear" },
        { "PE", "Pear" },
        { "Marquise", "Marquise" },
        { "MQ", "Marquise" },
        { "Radiant", "Radiant" },
        { "RA", "Radiant" },
        { "Oval", "Oval" },
        { "OV", "Oval" },
        { "Round", "Round" },
        { "RD", "Round" },
    };


    public static readonly HashSet<string> ProductSides = new()
    {
        "top",
        "side1",
        "side2",
        "top_hd_zo",
        "top_1carat",
        "top_2carat",
        "addl1",
        "addl2"
    };

    public readonly string ProductShapeImages = "product_shape_images['{0}']";
    public readonly string ProductImages = "product_images['{0}']";

    public readonly static Regex ProductCodeRegex =
        new(@"product_video_dict\[['""](?<type>[A-Z]+)['""]\]\s*=\s*'(?<code>\d+')",
            RegexOptions);


    public IEnumerable<ContentItem> GetImages(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scripts = doc
            .QuerySelectorAll("script")
            .Where(x => x.InnerText.Contains("image.brilliantearth.com"))
            .First(x => x.InnerText.Contains("product_shape_images"))
            .InnerText;

        var shapeMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var imagesMap = new Dictionary<string, ContentItem>(StringComparer.InvariantCultureIgnoreCase);
        var codes = ProductCodeRegex.Matches(scripts);

        foreach (Match match in codes)
        {
            shapeMap.Add(match.Groups["type"].Value, match.Groups["code"].Value);
        }

        var result = DiamondShapes
            .SelectMany(x => GetJsonString(string.Format(ProductShapeImages, x), scripts))
            .ToArray();

        result = ProductSides
            .SelectMany(x => GetJsonString(string.Format(ProductImages, x), scripts))
            .Union(result)
            .ToArray();


        return result;
    }

    public IEnumerable<ContentItem> GetJsonString(string prefix, string scripts)
    {
        // var prefix = string.Format(ProductShapeImages, diamondShape);
        var start = scripts.IndexOf(prefix, StringComparison.Ordinal);
        var regex = new Regex(@"""(?<size>lg)""\s*:\s*[""'](?<link>[A-Z\./0-9_:]+)[""']", RegexOptions);
        if (start < 0)
        {
            yield break;
        }

        var text = scripts[start..];
        var end = text.IndexOf("};", StringComparison.Ordinal) + 1;
        var jsonString = text[..end];

        var matches = regex.Matches(jsonString);

        foreach (Match match in matches)
        {
            yield return new ContentItem()
            {
                Type = ContentType.Image,
                Uri = UriFromString(match.Groups["link"].Value)
            };
        }
    }
}