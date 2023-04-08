using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Crawler.Core;

public static class ContentHelper
{
    public static async Task WriteAsync(RingSummary item, ImageDownloaderConfig config, CancellationToken token = default)
    {
        var path = config.OutputFolder;
        var baseFolder = Path.Combine(path, item.Category, item.Upc);
        var settings = new JsonSerializerOptions()
        {
            WriteIndented = true,
        };
        
        settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var json = JsonSerializer.Serialize(item,settings);

        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        await File.WriteAllTextAsync(
            Path.Combine(baseFolder, $"{item.Upc}.json"), json, token);
        
        await File.WriteAllTextAsync(
            Path.Combine(baseFolder, $"{item.Upc}.html"), item.HtmlSource, token);
    }
}