using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Habbo.Camera;
using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>Reads a .hab into a library. Two kinds: a sheet library (<c>name.json</c> and its TexturePacker sheet:
/// furni, figure parts, cursors), and a converted Flash library (XML documents and one image per asset: room
/// content).</summary>
public static class HabLibraryReader
{
    public static BundleLibrary Read(HabFile hab, string fallbackName)
    {
        var sheetJson = hab.Entries.Values.FirstOrDefault(e => e.MimeType == "application/json" && IsSheetLibrary(e.Bytes));
        return sheetJson is null ? ReadFlashLibrary(hab, fallbackName) : ReadSheetLibrary(hab, sheetJson, fallbackName);
    }

    private static bool IsSheetLibrary(byte[] json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("spritesheet", out _);
    }

    private static BundleLibrary ReadSheetLibrary(HabFile hab, HabEntry jsonEntry, string fallbackName)
    {
        var root = JsonNode.Parse(jsonEntry.Bytes)!.AsObject();
        var name = root["name"]?.GetValue<string>() ?? hab.Name ?? fallbackName;
        var prefix = (root["documentClass"]?.GetValue<string>() ?? name) + "_";
        var sheetNode = root["spritesheet"]!.AsObject();
        var imageName = sheetNode["meta"]?["image"]?.GetValue<string>();
        var sheetEntry = (imageName is not null && hab.Entries.TryGetValue(imageName, out var named) ? named : null)
            ?? hab.Entries.Values.FirstOrDefault(e => e.MimeType.StartsWith("image/", StringComparison.Ordinal))
            ?? throw new InvalidDataException($"{name} has a sheet but no image.");

        var library = new BundleLibrary(name);
        using var sheet = Bitmaps.Decode(sheetEntry.Bytes, sheetEntry.Name);
        var frames = new Dictionary<string, SheetFrame>(StringComparer.Ordinal);
        foreach (var (key, frame) in sheetNode["frames"]?.AsObject() ?? [])
        {
            if (frame!["rotated"]?.GetValue<bool>() == true)
                throw new NotSupportedException($"{name}: the rotated frame {key} is not supported.");
            frames[key.StartsWith(prefix, StringComparison.Ordinal) ? key[prefix.Length..] : key] = SheetFrame.Read(frame);
        }

        var drawn = new HashSet<string>(StringComparer.Ordinal);
        var paletted = new JsonArray();
        foreach (var (assetName, assetNode) in root["assets"]?.AsObject() ?? [])
        {
            var asset = assetNode!.AsObject();
            // drawn in a palette chosen when shown (pets): the document names them, the images stay as they are
            if (asset["usesPalette"]?.GetValue<bool>() == true)
                paletted.Add(assetName);
            var source = asset["source"]?.GetValue<string>();
            var hasOffset = asset.ContainsKey("x") || asset.ContainsKey("y");
            var (offsetX, offsetY) = (asset["x"]?.GetValue<int>() ?? 0, asset["y"]?.GetValue<int>() ?? 0);
            var (flipH, flipV) = (asset["flipH"]?.GetValue<bool>() == true, asset["flipV"]?.GetValue<bool>() == true);
            if (source is not null && source != assetName)
            {
                library.Images.Add(new BundleImage(assetName, null, source, offsetX, offsetY, hasOffset, flipH, flipV));
            }
            else if (frames.TryGetValue(assetName, out var frame))
            {
                // Habbo mirrors only an image drawn from another (GraphicAssetCollection): on its own pixels a flip is ignored
                library.Images.Add(new BundleImage(assetName, frame.Cut(sheet), null, offsetX, offsetY, hasOffset, false, false));
                drawn.Add(assetName);
            }
        }
        // frames no asset names are images still, without an offset
        foreach (var (frameName, frame) in frames.Where(f => !drawn.Contains(f.Key) && library.Images.All(i => i.Name != f.Key)))
            library.Images.Add(new BundleImage(frameName, frame.Cut(sheet), null, 0, 0, false, false, false));

        root.Remove("assets");
        root.Remove("spritesheet");
        if (paletted.Count > 0)
            root["paletteAssets"] = paletted;
        library.Documents.Add(new BundleDocument(name, BundleDocumentKind.Json, root.ToJsonString()));
        return library;
    }

    private static BundleLibrary ReadFlashLibrary(HabFile hab, string fallbackName)
    {
        var library = new BundleLibrary(hab.Name ?? fallbackName);
        var offsets = hab.Entries.TryGetValue("manifest", out var manifest) ? ManifestOffsets(manifest.Bytes) : [];
        var assets = AssetsDocument(hab);
        foreach (var entry in hab.Entries.Values)
        {
            if (entry.MimeType.StartsWith("image/", StringComparison.Ordinal))
            {
                // the library's own <assets> document says where an image sits, over its manifest
                var (x, y, hasOffset) = assets.TryGetValue(entry.Name, out var asset) && asset.HasOffset
                    ? (asset.X, asset.Y, true)
                    : offsets.TryGetValue(entry.Name, out var offset) ? (offset.X, offset.Y, true) : (0, 0, false);
                library.Images.Add(new BundleImage(entry.Name, Bitmaps.Decode(entry.Bytes, entry.Name), null, x, y, hasOffset, false, false));
            }
            else if (DocumentKind(entry.MimeType) is { } kind)
            {
                library.Documents.Add(new BundleDocument(entry.Name, kind, Text(entry.Bytes)));
            }
        }
        // and which images are drawn from another, maybe flipped, as Habbo reads it (GraphicAssetCollection)
        foreach (var (name, asset) in assets)
        {
            if (asset.Source is { } source && source != name && library.Images.All(i => i.Name != name))
                library.Images.Add(new BundleImage(name, null, source, asset.X, asset.Y, asset.HasOffset, asset.FlipH, asset.FlipV));
        }
        return library;
    }

    private sealed record AssetEntry(int X, int Y, bool HasOffset, string? Source, bool FlipH, bool FlipV);

    /// <summary>A Flash library's <c>&lt;assets&gt;</c> document (`room_assets`, `<type>_assets`): each asset's offset,
    /// and its source and flips when it is drawn from another.</summary>
    private static Dictionary<string, AssetEntry> AssetsDocument(HabFile hab)
    {
        var assets = new Dictionary<string, AssetEntry>(StringComparer.Ordinal);
        foreach (var entry in hab.Entries.Values.Where(e => DocumentKind(e.MimeType) == BundleDocumentKind.Xml))
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(Text(entry.Bytes));
            }
            catch (System.Xml.XmlException)
            {
                continue;
            }
            if (document.Root?.Name.LocalName != "assets")
                continue;
            foreach (var asset in document.Root.Elements("asset"))
            {
                if ((string?)asset.Attribute("name") is not { } name)
                    continue;
                var hasOffset = asset.Attribute("x") is not null || asset.Attribute("y") is not null;
                var source = (string?)asset.Attribute("source");
                assets[name] = new AssetEntry((int?)asset.Attribute("x") ?? 0, (int?)asset.Attribute("y") ?? 0, hasOffset,
                    string.IsNullOrEmpty(source) ? null : source,
                    (string?)asset.Attribute("flipH") is "1" or "true", (string?)asset.Attribute("flipV") is "1" or "true");
            }
        }
        return assets;
    }

    /// <summary>The <c>offset="x,y"</c> params of a manifest's assets.</summary>
    internal static Dictionary<string, (int X, int Y)> ManifestOffsets(byte[] manifest)
    {
        var offsets = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
        foreach (var asset in XDocument.Parse(Text(manifest)).Descendants("asset"))
        {
            var param = asset.Elements("param").FirstOrDefault(p => (string?)p.Attribute("key") == "offset");
            if ((string?)asset.Attribute("name") is { } name && (string?)param?.Attribute("value") is { } value
                && ManifestParams.TryParsePoint(value, out var point))
                offsets[name] = point;
        }
        return offsets;
    }

    internal static BundleDocumentKind? DocumentKind(string mimeType) => mimeType switch
    {
        "text/xml" or "application/xml" => BundleDocumentKind.Xml,
        "application/json" => BundleDocumentKind.Json,
        "text/plain" => BundleDocumentKind.Text,
        _ => null,
    };

    internal static string Text(byte[] data) => new System.Text.UTF8Encoding(false).GetString(data).TrimStart('﻿');
}
