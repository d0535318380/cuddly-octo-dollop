using System.Text.RegularExpressions;

namespace Crawler.Core;

public enum ContentType
{
    Image = 1,
    Video ,
    Jsonp,
    View3d,
    Html
}

public class ContentItem : ItemBase
{
    public string Folder { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public ContentItem()
    {
        Type = ContentType.Image;
    }

}