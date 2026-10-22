using System.Text.RegularExpressions;
using System.Xml.Linq;
using Habbo.Infrastructure;

namespace Habbo.Tools.AssetMirror;

/// <summary>Where each kind of asset is, worked out from the gamedata the way the client works it out.</summary>
public static partial class AssetUrls
{
    private const string DefaultFurniTemplate = "%revision%/%typeid%.swf";

    /// <summary>The libraries the web client loads: Habbo keeps each library's .hab beside its .swf, and only the
    /// .hab is mirrored (the clients this hotel serves read nothing else).</summary>
    public static string Hab(string template) =>
        template.EndsWith(".swf", StringComparison.OrdinalIgnoreCase) ? template[..^4] + ".hab" : template;

    /// <summary>The files beside figuremap.xml the client loads by name: the avatar's actions, and the base libraries
    /// every figure is drawn from.</summary>
    private static readonly string[] AvatarClientFiles = ["HabboAvatarActions.xml", "hh_human_body.hab", "hh_human_item.hab"];

    public static IEnumerable<string> AvatarClient(string clientUrl) => AvatarClientFiles.Select(name => clientUrl + name);

    /// <summary>The habbicons: their index and sprite sheets, under habbicons.asset.root and habbicons.hash.</summary>
    public static IEnumerable<string> Habbicons(string root, string hash)
    {
        var baseUrl = $"{root.TrimEnd('/')}/{hash}/";
        string[] files = ["habbicons.json", "habbicons_spritesheet.png", "collection_icons_spritesheet.png"];
        return files.Select(name => baseUrl + name);
    }

    /// <summary>A habbicon's animation, by its id (most have none).</summary>
    public static IEnumerable<string> HabbiconAnimations(string root, string hash, IEnumerable<int> ids) =>
        ids.Distinct().Order().Select(id => $"{root.TrimEnd('/')}/{hash}/animation/{id.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png");

    /// <summary>The images variables name relative to the image library (billboard.adwarning.left.url =
    /// AdWarningsUK/ad_warning_L.png), as the client joins them to image.library.url.</summary>
    public static IEnumerable<string> RelativeImages(ClientVariables variables)
    {
        var library = variables.Url("image.library.url", "//images.habbo.com/c_images/");
        return variables.All()
            .Select(v => v.Value)
            .Where(value => IsImage(value) && !value.Contains("//", StringComparison.Ordinal) && !value.Contains(' ') && !value.StartsWith('$'))
            .Select(value => library + value.TrimStart('/'))
            .Distinct()
            .Order(StringComparer.Ordinal);
    }
    private const string DefaultIconTemplate = "%revision%/%typeid%%param%_icon.png";
    private const string DefaultLibraryTemplate = "%libname%.swf";
    private const string DefaultSampleTemplate = "mp3/sound_machine_sample_%typeid%.mp3";
    private const string DefaultBadgePath = "//images.habbo.com/c_images/album1584";

    /// <summary>What sets a colour variant apart in its class name: "chair_norja*2".</summary>
    private const char ColourSeparator = '*';

    /// <summary>Every furni's library (by its base class) and icon (colour variants have their own), under its revision.</summary>
    public static IEnumerable<string> Furni(byte[] furnidata, ClientVariables variables)
    {
        var baseUrl = variables.Url("flash.dynamic.download.url");
        var swf = Hab(variables.Get("flash.dynamic.download.name.template", DefaultFurniTemplate));
        var icon = variables.Get("flash.dynamic.icon.download.name.template", DefaultIconTemplate);

        using var stream = new MemoryStream(furnidata);
        foreach (var definition in FurnidataParser.Parse(stream))
        {
            var separator = definition.ClassName.IndexOf(ColourSeparator);
            var name = separator < 0 ? definition.ClassName : definition.ClassName[..separator];
            var colour = separator < 0 ? string.Empty : "_" + definition.ClassName[(separator + 1)..];
            var revision = definition.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);

            yield return baseUrl + swf.Replace("%revision%", revision).Replace("%typeid%", name);
            yield return baseUrl + icon.Replace("%revision%", revision).Replace("%typeid%", name).Replace("%param%", colour);
        }
    }

    /// <summary>The libraries a map (figuremap.xml, effectmap.xml) names, next to the map itself.</summary>
    public static IEnumerable<string> Libraries(string mapUrl, byte[] map, string element, string attribute, string template = DefaultLibraryTemplate)
    {
        var baseUrl = mapUrl[..(mapUrl.LastIndexOf('/') + 1)];
        using var stream = new MemoryStream(map);
        return XDocument.Load(stream).Descendants(element)
            .Select(e => (string?)e.Attribute(attribute))
            .OfType<string>()
            .Where(name => name.Length > 0)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(name => baseUrl + Hab(template).Replace("%libname%", name))
            .ToList();
    }

    public static IEnumerable<string> Pets(string clientUrl, ClientVariables variables) =>
        variables.Get("pet.configuration").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(pet => clientUrl + Hab(DefaultLibraryTemplate).Replace("%libname%", pet));

    /// <summary>Every badge that has a name in external_texts, as a gif.</summary>
    public static IEnumerable<string> Badges(string texts, ClientVariables variables)
    {
        var path = variables.Url("badge.image.path", DefaultBadgePath).TrimEnd('/');
        return BadgeName().Matches(texts).Select(m => m.Groups[1].Value)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(code => $"{path}/{code}.gif");
    }

    public static IEnumerable<string> Sounds(ClientVariables variables, int maxSample)
    {
        var baseUrl = variables.Url("flash.dynamic.download.url");
        var template = variables.Get("flash.dynamic.download.samples.template", DefaultSampleTemplate);
        return Enumerable.Range(1, maxSample).Select(n => baseUrl + template.Replace("%typeid%", n.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>Every image on the image server that the gamedata names: landing view promos, reception and hotel view
    /// art, notification icons and the like, the client variables in them filled in first.</summary>
    public static IEnumerable<string> ImageReferences(IEnumerable<string> texts, ClientVariables variables) =>
        texts.SelectMany(text => ImageUrl().Matches(variables.Expand(text)))
            .Select(m => m.Value.StartsWith("//", StringComparison.Ordinal) ? "https:" + m.Value : m.Value.Replace("http://", "https://"))
            .Distinct()
            .Order(StringComparer.Ordinal);

    /// <summary>The catalogue page icons, which are numbered: icon_1.png onwards, up to the number asked.</summary>
    public static IEnumerable<string> CatalogueIcons(ClientVariables variables, int maxIcon)
    {
        var baseUrl = variables.Url("image.library.catalogue.url", "//images.habbo.com/c_images/catalogue/");
        return Enumerable.Range(1, maxIcon).Select(n => $"{baseUrl}icon_{n.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png");
    }

    /// <summary>The group badge parts, by the file names the hotel gives them (base_basic_1.gif is badgepart_base_basic_1.png).</summary>
    public static IEnumerable<string> BadgeParts(IEnumerable<string> fileNames, ClientVariables variables)
    {
        var baseUrl = variables.Url("image.library.badgepart.url", "//images.habbo.com/c_images/Badgeparts/");
        return fileNames.Where(name => name.Length > 0)
            .Select(name => $"{baseUrl}badgepart_{Path.GetFileNameWithoutExtension(name)}.png")
            .Distinct()
            .Order(StringComparer.Ordinal);
    }

    /// <summary>The help pages a text names ("habbopages/chat/commands", or "event:habbopages/..." in a page's links).</summary>
    public static IEnumerable<string> HabbopageNames(string text) =>
        HabbopageLink().Matches(text).Select(m => m.Groups[1].Value.Trim('.', '/'))
            .Where(name => name.Length > 0 && !name.Contains("..", StringComparison.Ordinal) && !IsImage(name))
            .Distinct(StringComparer.Ordinal);

    /// <summary>The images a help page shows, as absolute urls.</summary>
    public static IEnumerable<string> PageImages(string page, Uri pageUrl) =>
        PageImage().Matches(page)
            .Select(m => Uri.TryCreate(pageUrl, m.Groups[1].Value.Trim(), out var url) ? url : null)
            .OfType<Uri>()
            .Where(url => url.Scheme is "http" or "https")
            .Select(url => url.Scheme == "http" ? "https" + url.AbsoluteUri[4..] : url.AbsoluteUri)
            .Distinct(StringComparer.Ordinal);

    private static bool IsImage(string name) =>
        name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

    /// <summary>"habbopages/..." as a page, not as a folder in an image's url (c_images/habbopages/...).</summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_\-./])habbopages/([A-Za-z0-9_\-./]+)")]
    private static partial Regex HabbopageLink();

    [GeneratedRegex(@"<img[^>]+src\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex PageImage();

    [GeneratedRegex(@"^badge_name_([^=\s]+)=", RegexOptions.Multiline)]
    private static partial Regex BadgeName();

    [GeneratedRegex(@"(?:https?:)?//images\.habbo\.com/[^\s""'<>,;)]+?\.(?:png|gif|jpe?g)", RegexOptions.IgnoreCase)]
    private static partial Regex ImageUrl();
}
