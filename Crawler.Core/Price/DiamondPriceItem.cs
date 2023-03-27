using System.Text.RegularExpressions;

namespace Crawler.Core;

public class DiamondPriceItem
{

    
    public string Cut { get; set; }
    public string Color { get; set; }
    public string Clarity { get; set; }
    public decimal MinSize { get; set; }
    public decimal MaxSize { get; set; }
    public decimal Amount { get; set; }

    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public static DiamondPriceItem Create(string cut, MatchCollection matches)
    {
  
        var instance = new DiamondPriceItem
        {
            Cut = cut,
            Color = GetString("color", matches),
            Clarity = GetString("clear", matches),
            MinSize = GetDecimal("min", matches),
            MaxSize = GetDecimal("max", matches),
            Amount = GetDecimal("value", matches)
        };

        instance.MinPrice = instance.Amount * instance.MinSize / (decimal) 0.01;
        instance.MaxPrice = instance.Amount * instance.MaxSize / (decimal) 0.01;
        
        return instance;
    }

    private static decimal GetDecimal(string name, MatchCollection matches)
    {
        var match = matches.First(x => x.Groups["name"].Value == name);

        return decimal.Parse(match.Groups["value"].Value);
    }
    
    private static string GetString(string name, MatchCollection matches)
    {
        var match = matches.First(x => x.Groups["name"].Value == name);

        return match.Groups["value"].Value;
    }

}