using System.Diagnostics;
using Crawler.Core;

const string sourceFolder = "Input";
const string outputFolder = "Output";

var stopWatch = new Stopwatch();
var sources = Directory.EnumerateFiles(sourceFolder);

stopWatch.Start();

foreach (var source in sources)
{
    var urls = await File.ReadAllLinesAsync(source);
    var itemsFolder = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(source));

    urls = urls.Distinct().ToArray();
    foreach (var url in urls)
    {
        var factory = new BrilliantEarthFactory();
        var items = await factory.GetItemsAsync(url);
        await ImageDownloader.DownloadAsync(items, itemsFolder);
    }
}

stopWatch.Stop();
Console.WriteLine(stopWatch.Elapsed.ToString("g"));
Console.WriteLine("Press any key.......................");
Console.ReadKey();

// var sourceUrl = "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/";