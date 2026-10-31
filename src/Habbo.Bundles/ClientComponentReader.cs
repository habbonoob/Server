using System.Text.RegularExpressions;
using System.Xml.Linq;
using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>Reads a component of the client binary into a library, from the release's decompiled source: the
/// component class's <c>[Embed]</c>s say which file each of its members is, and its manifest which members are its
/// assets. A manifest asset with a <c>ref</c> is that member's file; a <c>region</c> param cuts it, an
/// <c>offset</c> param is its offset. Assets the manifest names but the binary never embedded are left out, as the
/// client finds none either. The manifest itself is kept as the document <c>manifest</c>.</summary>
public static partial class ClientComponentReader
{
    public static BundleLibrary Read(string sourceDirectory, string component)
    {
        if (!ComponentName().IsMatch(component))
            throw new ArgumentException($"{component} is not a component name.", nameof(component));
        var classFile = Directory.EnumerateFiles(sourceDirectory, component + ".as", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new FileNotFoundException($"The client has no component {component}.");

        var embeds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match embed in EmbedPattern().Matches(File.ReadAllText(classFile)))
            embeds[embed.Groups["member"].Value] = Path.Combine(sourceDirectory, embed.Groups["file"].Value.TrimStart('/'));
        if (!embeds.TryGetValue("manifest", out var manifestFile))
            throw new InvalidDataException($"{component} embeds no manifest.");

        var library = new BundleLibrary(component);
        var manifestText = HabLibraryReader.Text(File.ReadAllBytes(manifestFile));
        library.Documents.Add(new BundleDocument("manifest", BundleDocumentKind.Xml, manifestText));

        var decoded = new Dictionary<string, SKBitmap>(StringComparer.Ordinal);
        try
        {
            foreach (var asset in XDocument.Parse(manifestText).Descendants("asset"))
            {
                var name = (string?)asset.Attribute("name");
                var mimeType = (string?)asset.Attribute("mimeType") ?? "";
                var member = (string?)asset.Attribute("ref") ?? name;
                if (name is null || member is null || !embeds.TryGetValue(member, out var file) || !File.Exists(file))
                    continue;

                if (HabLibraryReader.DocumentKind(mimeType) is { } kind)
                {
                    library.Documents.Add(new BundleDocument(name, kind, HabLibraryReader.Text(File.ReadAllBytes(file))));
                }
                else if (mimeType.StartsWith("image/", StringComparison.Ordinal))
                {
                    if (!decoded.TryGetValue(file, out var whole))
                        decoded[file] = whole = Bitmaps.Decode(File.ReadAllBytes(file), file);
                    var param = (string key) => (string?)asset.Elements("param").FirstOrDefault(p => (string?)p.Attribute("key") == key)?.Attribute("value");
                    var region = param("region") is { } r && ManifestParams.TryParseRegion(r, out var rect)
                        ? rect
                        : new SKRectI(0, 0, whole.Width, whole.Height);
                    var hasOffset = param("offset") is { } o && ManifestParams.TryParsePoint(o, out _);
                    ManifestParams.TryParsePoint(param("offset") ?? "", out var offset);
                    library.Images.Add(new BundleImage(name, Bitmaps.Crop(whole, region), null, offset.X, offset.Y, hasOffset,
                        param("flipH") is "1" or "true", param("flipV") is "1" or "true"));
                }
            }
        }
        finally
        {
            foreach (var bitmap in decoded.Values)
                bitmap.Dispose();
        }
        return library;
    }

    /// <summary>An <c>[Embed(source="…")]</c> and the member it binds, whatever the member's access and kind (a
    /// component's are <c>public static</c>, a class's own often <c>private var</c>).</summary>
    [GeneratedRegex("""\[Embed\(source="(?<file>[^"]+)"[^\]]*\]\s*(?:(?:public|private|protected|internal|static)\s+)*(?:var|const)\s+(?<member>\w+)\s*:\s*Class""")]
    internal static partial Regex EmbedPattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex ComponentName();
}
