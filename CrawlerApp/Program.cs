// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Downloader;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;


var line =
    "<td " +
    "data-min=\"0.70\" " +
    "data-max=\"0.89\" " +
    "data-value=\"62\" " +
    "data-type=\"фантазийный\" " +
    "data-color=\"D\" " +
    "data-clear=\"IF\">62</td>";
// var matches = price.Matches(line);


// From Web
var url = "https://www.brilliantearth.com/Gala-Diamond-Ring-White-Gold-BE1D6362P-9634461/";
var web = new HtmlWeb();
var doc = web.Load(url);

var regex = new Regex("(?<sku>[A-Z0-9]+[-]?[A-Z0-9]+)(?<id>[\\d+])/$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
var price = new Regex("data-(?<name>min|max|value|color|clear)=\"(?<value>[A-Z.\\d]+)\"",
    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);


var sku = regex.Match(url).Groups["sku"].Value;

if (!Directory.Exists(sku))
{
    Directory.CreateDirectory(sku);
    Directory.CreateDirectory(Path.Combine(sku, "Rotato"));
}

var root = doc
    .QuerySelectorAll("div.ir328-pdp-gallery")
    .FirstOrDefault();

var childs = root.QuerySelectorAll("img.img-responsive");
var links = childs.Select(x => 
    x.Attributes["src"].Value
    .Replace("https://", "")
    .Replace("//", ""));
var iframeNode = root.QuerySelector("iframe");
    
var iframeUri = iframeNode.Attributes["src"]
    .Value
    .Replace("https://", "")
    .Replace("//", "");
 


// TODO: Parse from javascript
var iframeWeb = new HtmlWeb();
HtmlAgilityPack.HtmlWeb.PreRequestHandler handler = delegate (HttpWebRequest request)
{
    request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
    request.CookieContainer = new System.Net.CookieContainer();
    return true;
};
iframeWeb.PreRequest += handler;

var iframeDoc = iframeWeb.Load("https://embed.imajize.com/5548150?v=1677668852");
// var rotateUrls = iframeDoc
//     .QuerySelector("ol.rotato")
//     .QuerySelectorAll("img")
//     .Select(x=>x.Attributes["src"].Value);

var iframeWebdownloader = new DownloadService();
iframeWebdownloader
    .DownloadFileTaskAsync("https://embed.imajize.com/5548150?v=1677668852", 
    Path.Combine(sku, "iframe.html"))
    .Wait();

// HttpClientHandler handler = new HttpClientHandler()
// {
//     AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
// };        
// var client = new HttpClient(handler);
// var resTask = client.GetAsync("https://embed.imajize.com/5548150?v=1677668852");
// var res = resTask.Result.Content.ReadAsStringAsync().Result;
// resTask.Wait();
var options = new ParallelOptions() { MaxDegreeOfParallelism  = 20 };


foreach (var link in links)
{
    Console.WriteLine(link);

    try
    {
        var uri = Path.GetFileName(link);
    
        var downloader = new DownloadService();
        var task = downloader.DownloadFileTaskAsync("https://" + link, Path.Combine(sku, uri));

        task.Wait();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

// var i = 1;
// foreach (var link in rotateUrls)
// {
//     Console.WriteLine($"{i}: link");
//
//     try
//     {
//         var uri = $"{sku}-{i}.jpg";
//     
//         var downloader = new DownloadService();
//         var task = downloader.DownloadFileTaskAsync(link, Path.Combine(sku, "rotato", uri));
//
//         task.Wait();
//     }
//     catch (Exception e)
//     {
//         Console.WriteLine(e);
//     }
// }

var inputStr = @"

";
string encodedStr = Convert.ToBase64String(Encoding.UTF8.GetBytes("inputStr"));
