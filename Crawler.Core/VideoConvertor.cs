using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace Crawler.Core;

public class VideoConvertor
{
    private static Regex regex = new Regex("\\d{3}[.](jpg|png)", RegexOptions.Compiled | RegexOptions.Multiline);

    public static async Task ConvertFolderAsync(string sourceFolder = "Output", CancellationToken token = default)
    {
        Console.WriteLine(FFmpeg.ExecutablesPath);
        
        var videoConvertor = new VideoConvertor();
        await videoConvertor.ConvertAsync(sourceFolder, token: token);
    }
    
    public async Task ConvertAsync(string sourceFolder = "Output", CancellationToken token = default)
    {
        var items = Directory.EnumerateDirectories(
            sourceFolder,
            "*.*",
            SearchOption.TopDirectoryOnly);

        foreach (var item in items)
        {
            await ConvertItemAsync(item, token);
        }
    }

    private static async Task ConvertItemAsync(string folder, CancellationToken token)
    {
        var items = Directory
            .EnumerateDirectories(folder, "*.*", SearchOption.AllDirectories)
            .Where(x => x.EndsWith("View3d"))
            .OrderBy(x=>x)
            .ToArray();

        foreach (var item in items)
        {
            //  fileName = regex.Replace(fileName, "%03d") + Path.GetExtension(fileName);

            try
            {
                await ConvertToVideo(item, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private static async Task ConvertToVideo(string folder, CancellationToken token)
    {
        var files = Directory.EnumerateFiles(folder).OrderBy(x => x).ToArray();

        if (!files.Any())
        {
            return;
        }

        var sourceFolder = Path.GetDirectoryName(files.First());
        var outputFolder = sourceFolder.Replace("View3d", "Video3d");
        var outputFile = Path.Combine(outputFolder, "video-100.mp4");
        var convertor = FFmpeg.Conversions.New();
        var percents = new[] { 0.5, 0.6, 0.7, 0.8, 0.9 };
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        Console.WriteLine("Start: {0}", sourceFolder);

        await convertor
            .BuildVideoFromImages(files)
            .SetOutput(outputFile)
            .UseMultiThread(true)
            .UseHardwareAcceleration(HardwareAccelerator.dxva2, VideoCodec.h264, VideoCodec.h264)
            .AddParameter("-vf \"crop=trunc(iw/2)*2:trunc(ih/2)*2\",scale=1024:1024,setsar=1:1")
            .Start(token);

        Console.WriteLine("Finish: {0}", outputFile);


        var tasks = percents.Select(x => ChangeSpeedAsync(outputFile, x, token));

        await Task.WhenAll(tasks);
        
        Console.WriteLine("Finish: {0}", sourceFolder);
    }

    private static Task ChangeSpeedAsync(string outputFile, double speed, CancellationToken token)
    {
        try
        {
            return ChangeSpeedInternalAsync(outputFile, speed, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine(outputFile);
            return Task.CompletedTask;
        }
    }


    private static async Task ChangeSpeedInternalAsync(string source, double speed, CancellationToken token)
    {
        var inputFile = await FFmpeg.GetMediaInfo(source, token);
        var outputFolder = Path.GetDirectoryName(source);
        var percent = (int)(speed * 100);
        var outputFile = Path.Combine(outputFolder, $"video-0{percent}.mp4");
        var videoStream = inputFile.VideoStreams.First().ChangeSpeed(speed);

        Console.WriteLine("Start: {0}", outputFile);

        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }
        
        await FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .SetOutput(outputFile)
            .UseMultiThread(true)
            .Start(token);
        
        Console.WriteLine("Finish: {0}", outputFile);
    }
}