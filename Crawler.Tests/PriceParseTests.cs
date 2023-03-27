using Crawler.Core;
using FluentAssertions;

namespace Crawler.Tests;

public class PriceParseTests
{
    [Fact]
    public async Task RoundPriceTest()
    {
        var items = await PriceParser.ReadAsync(
            "Round", 
            @"Data\price-round.html");

        await PriceParser.WriteAsync(items.ToArray(), @"Data\price-round.csv");
        
        items
            .Should()
            .NotBeEmpty();
    }
    
    [Fact]
    public async Task PearPriceTest()
    {
        var items = await PriceParser.ReadAsync(
            "Pear", 
            @"Data\price-pear.html");

        
        await PriceParser.WriteAsync(items.ToArray(), @"Data\price-pear.csv");
        
        items
            .Should()
            .NotBeEmpty();
    }
}