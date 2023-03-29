using System.Diagnostics;
using Crawler.Core;

const string sourceFolder = "Input";
const string outputFolder = "Output";

var url = "https://www.brilliantearth.com/Luxe-Secret-Garden-Diamond-Ring-(3/4-ct.-tw.)-White-Gold-BE1D6352-12137267/";
var stopWatch = new Stopwatch();

stopWatch.Start();

var factory = new BrilliantEarthFactory();
var items = await factory.GetItemsAsync(url);
await ImageDownloader.DownloadAsync(items, outputFolder);

// var sources = Directory.EnumerateFiles(sourceFolder);
// foreach (var source in sources)
// {
//     var urls = await File.ReadAllLinesAsync(source);
//     var itemsFolder = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(source));
//
//     urls = urls.Distinct().ToArray();
//     foreach (var url in urls)
//     {
//         var factory = new BrilliantEarthFactory();
//         var items = await factory.GetItemsAsync(url);
//         await ImageDownloader.DownloadAsync(items, itemsFolder);
//     }
// }

stopWatch.Stop();
Console.WriteLine(stopWatch.Elapsed.ToString("g"));
Console.WriteLine("Press any key.......................");
Console.ReadKey();

// var sourceUrl = "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/";