using System.Diagnostics;
using Crawler.Core;

const string sourceFolder = "Input";
const string outputFolder = "Output";
const bool convertVideo = true;
const bool downloadImages = true;
const bool fromSourceFile = false;

var config = new ImageDownloaderConfig()
{
    DownloadImages = true,
    DownloadVideos = true,
    DownloadView3d = true,
    
};

var sources = new[] { "engagements" };
var sourceUrls = new[]
{
  //  "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/"
  "https://www.brilliantearth.com/Diamond-Tennis-Bracelet-(1-ct.-tw.)-White-Gold-BE5D10TB/"
};

if (downloadImages)
{
    var stopWatch = new Stopwatch();

    if (fromSourceFile)
    {
        sources = Directory.EnumerateFiles(sourceFolder).ToArray();    
    }

    stopWatch.Start();
    foreach (var source in sources)
    {
        var urls = fromSourceFile ? await File.ReadAllLinesAsync(source) : sourceUrls;
        config.OutputFolder = outputFolder;
        
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
}


if (convertVideo)
{
    await VideoConvertor.ConvertFolderAsync(outputFolder);
}


Console.WriteLine("Press any key.......................");
//Console.ReadKey();

// var sourceUrl = "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/";