﻿using System.Diagnostics;
using Crawler.Core;
using OpenQA.Selenium.DevTools.V110.Storage;

const string sourceFolder = "Input";
const string outputFolder = "c://__Output";
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
    "https://www.brilliantearth.com/Gala-Diamond-Ring-Gold-BE1D6362P-9634467/",
    "https://www.brilliantearth.com/Petite-Comfort-Fit-Wedding-Ring-Gold-BE299/",
    "https://www.brilliantearth.com/Round-Diamond-Stud-Earrings-(1-ct.-tw.)-White-Gold-BE304RD100/",
    "https://www.brilliantearth.com/Diamond-Tennis-Bracelet-(1-ct.-tw.)-White-Gold-BE5D10TB/",
    "https://www.brilliantearth.com/Heart-Shaped-Lab-Diamond-Pendant-Silver-BE4LD899/"
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
            var items = await factory.GetItemsAsync(url, config);
            var uniqueCodes = items.Select(x => x.Upc).Distinct().ToArray();
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