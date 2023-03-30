using System.Diagnostics;
using Crawler.Core;

const string sourceFolder = "Input";
const string outputFolder = "Output";

//var url = "https://www.brilliantearth.com/Luxe-Secret-Garden-Diamond-Ring-(3/4-ct.-tw.)-White-Gold-BE1D6352-12137267/";
// var url = "https://www.brilliantearth.com/Four-prong-Round-Diamond-Stud-Earrings-White-Gold-BE304RD-1151787/";
// var url = "https://www.brilliantearth.com/Floating-Solitaire-Pendant-Platinum-BE403-1151865";


var fromSourceFile = true;
var sources = new[] { "engagements" };
var sourceUrls = new[]
{
    "https://www.brilliantearth.com/Dewdrop-Diamond-Pendant-Silver-BE4D101D/"
};

var stopWatch = new Stopwatch();

if (fromSourceFile)
{
     sources = Directory.EnumerateFiles(sourceFolder).ToArray();    
}

stopWatch.Start();
foreach (var source in sources)
{
    var urls = fromSourceFile ? await File.ReadAllLinesAsync(source) : sourceUrls;
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