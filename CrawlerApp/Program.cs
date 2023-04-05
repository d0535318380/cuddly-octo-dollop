using System.Diagnostics;
using Crawler.Core;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

const string sourceFolder = "Input";
const string outputFolder = "Output";
const bool convertVideo = true;
const bool downloadImages = false;

var fromSourceFile = true;
var config = new ImageDownloaderConfig()
{
    DownloadImages = true,
    DownloadVideos = true,
    DownloadView3d = true,
    
};

var sources = new[] { "engagements" };
var sourceUrls = new[]
{
    "https://www.brilliantearth.com/Double-Hidden-Halo-Diamond-Ring-(1/6-ct.-tw.)-White-Gold-BE1D3410-12777085/"
};

if (convertVideo)
{
    await VideoConvertor.ConvertFolderAsync();
}

if (!downloadImages)
{
    return;
}


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
        await ImageDownloader.DownloadAsync(items, config);
    }
}

stopWatch.Stop();
Console.WriteLine(stopWatch.Elapsed.ToString("g"));
Console.WriteLine("Press any key.......................");
Console.ReadKey();

// var sourceUrl = "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/";