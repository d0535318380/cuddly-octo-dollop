namespace Crawler.Core;

public struct ContentType
{
    public const string Image = "image";
    public const string Video = "video";
    public const string Jsonp = "jsonp";
    public const string Html = "html";
}

public class ContentItem : ItemBase
{
    public ContentItem()
    {
        Type = ContentType.Image;
    }
}