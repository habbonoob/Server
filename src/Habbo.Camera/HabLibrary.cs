using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using SkiaSharp;

namespace Habbo.Camera;

/// <summary>One asset library, read from its .hab as the client reads it (habbo-pixi's hab.ts):
///  - a room object (furni, pets, avatar parts, effects): "name.json", whose assets name the frame of a sprite
///    sheet ("name.png") they are cut from, an alias following its source;
///  - a converted Flash library (the room's planes): one image entry per asset, and an "*_assets" XML naming the
///    aliases (another asset, flipped).
/// Bitmaps are cut once and kept with the library.</summary>
public sealed class HabLibrary : IDisposable
{
    /// <summary>How far an alias is followed to the asset it draws, as js-client does.</summary>
    private const int MaxSourceSteps = 8;

    private readonly Dictionary<string, AssetDefinition> _assets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HabEntry> _entries;
    private readonly ConcurrentDictionary<string, SKBitmap?> _bitmaps = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SKBitmap?> _sheets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _columnWidths = new(StringComparer.Ordinal);

    /// <summary>Skia's bitmaps are not for drawing from on several threads at once while being decoded.</summary>
    private readonly Lock _draw = new();

    private HabLibrary(HabFile file)
    {
        _entries = new Dictionary<string, HabEntry>(file.Entries, StringComparer.Ordinal);
        foreach (var entry in file.Entries.Values)
        {
            if (entry.MimeType == "application/json")
                ReadRoomObject(entry);
            else if (entry.MimeType.Contains("xml", StringComparison.Ordinal) && entry.Name.EndsWith("_assets", StringComparison.Ordinal))
                ReadAssetsXml(entry);
            else if (entry.MimeType.Contains("xml", StringComparison.Ordinal) && entry.Name.EndsWith("_visualization", StringComparison.Ordinal))
                ReadMaterials(entry);
        }
        // a converted library's images are assets by their own names
        foreach (var entry in file.Entries.Values.Where(e => e.MimeType.StartsWith("image/", StringComparison.Ordinal)))
            _assets.TryAdd(entry.Name, new AssetDefinition(null, false, false, 0, 0, entry.Name, null));
    }

    public static HabLibrary Read(byte[] data) => new(HabFile.Read(data));

    public bool Has(string name) => _assets.ContainsKey(name);

    public IEnumerable<string> AssetNames => _assets.Keys;

    /// <summary>The asset's pixels (an alias's are its source's) with how the library has it drawn. Null when the
    /// library has no such asset, or no pixels for it.</summary>
    public LibraryAsset? Get(string name)
    {
        if (!_assets.TryGetValue(name, out var asset))
            return null;
        var bitmap = _bitmaps.GetOrAdd(SourceOf(name), Cut);
        return bitmap is null ? null : new LibraryAsset(bitmap, asset.FlipH, asset.FlipV, asset.X, asset.Y);
    }

    /// <summary>How wide a plane's texture column drawn from this asset is (its material's materialCellColumn
    /// width): the column is cut to it, whatever the asset's own width. Null when no material here draws it.</summary>
    public int? ColumnWidth(string name) => _columnWidths.TryGetValue(name, out var width) ? width : null;

    /// <summary>A data entry's bytes (a pet's palette), by its name.</summary>
    public byte[]? GetBytes(string name) =>
        _entries.TryGetValue(name, out var entry) ? entry.Bytes
        : _entries.Values.FirstOrDefault(e => !e.MimeType.StartsWith("image/", StringComparison.Ordinal)
                                              && e.Name.EndsWith("_" + name, StringComparison.Ordinal))?.Bytes;

    private string SourceOf(string name)
    {
        var at = name;
        for (var i = 0; i < MaxSourceSteps; i++)
        {
            if (!_assets.TryGetValue(at, out var asset) || string.IsNullOrEmpty(asset.Source) || asset.Source == at)
                return at;
            at = asset.Source;
        }
        return at;
    }

    private SKBitmap? Cut(string name)
    {
        if (!_assets.TryGetValue(name, out var asset) || asset.Image is null)
            return null;
        var sheet = _sheets.GetOrAdd(asset.Image, Decode);
        if (sheet is null)
            return null;
        if (asset.Frame is not { } frame)
            return sheet;

        // the frame placed at its trim offset in a canvas of its untrimmed size
        var bitmap = new SKBitmap(new SKImageInfo(Math.Max(1, frame.SourceWidth), Math.Max(1, frame.SourceHeight), SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        lock (_draw)
            canvas.DrawBitmap(sheet, new SKRect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height),
                new SKRect(frame.TrimX, frame.TrimY, frame.TrimX + frame.Width, frame.TrimY + frame.Height), new SKSamplingOptions(SKFilterMode.Nearest), null);
        return bitmap;
    }

    private SKBitmap? Decode(string image)
    {
        if (!_entries.TryGetValue(image, out var entry))
            return null;
        lock (_draw)
            return SKBitmap.Decode(entry.Bytes);
    }

    private void ReadRoomObject(HabEntry entry)
    {
        using var json = JsonDocument.Parse(entry.Bytes);
        var root = json.RootElement;
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Object)
            return;
        var sheet = root.TryGetProperty("spritesheet", out var s) ? s : default;
        var frames = sheet.ValueKind == JsonValueKind.Object && sheet.TryGetProperty("frames", out var f) ? f : default;
        var image = sheet.ValueKind == JsonValueKind.Object && sheet.TryGetProperty("meta", out var meta)
                    && meta.TryGetProperty("image", out var i) ? i.GetString() ?? "" : "";
        var sheetBase = Path.GetFileNameWithoutExtension(image);
        var prefix = root.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } objectName ? objectName : sheetBase;

        foreach (var property in assets.EnumerateObject())
        {
            var a = property.Value;
            SheetFrame? frame = null;
            // its frame, by the names js-client tries: the object's prefix, the sheet's, then its own
            if (frames.ValueKind == JsonValueKind.Object)
                foreach (var key in new[] { prefix.Length > 0 ? $"{prefix}_{property.Name}" : "", sheetBase.Length > 0 ? $"{sheetBase}_{property.Name}" : "", property.Name })
                    if (key.Length > 0 && frames.TryGetProperty(key, out var found))
                    {
                        frame = SheetFrame.From(found);
                        break;
                    }

            _assets[property.Name] = new AssetDefinition(
                a.TryGetProperty("source", out var source) ? source.GetString() : null,
                a.TryGetProperty("flipH", out var flipH) && flipH.ValueKind == JsonValueKind.True,
                a.TryGetProperty("flipV", out var flipV) && flipV.ValueKind == JsonValueKind.True,
                a.TryGetProperty("x", out var x) && x.ValueKind == JsonValueKind.Number ? (int)x.GetDouble() : 0,
                a.TryGetProperty("y", out var y) && y.ValueKind == JsonValueKind.Number ? (int)y.GetDouble() : 0,
                frame is null || image.Length == 0 ? null : image,
                frame);
        }
    }

    /// <summary>The room's materials (room_visualization): which texture each column draws, and how wide the column
    /// is; a texture's bitmaps are its assets, one for each way a plane can face.</summary>
    private void ReadMaterials(HabEntry entry)
    {
        var document = XDocument.Parse(Encoding.Latin1.GetString(entry.Bytes));
        var textures = document.Descendants("texture")
            .Where(t => (string?)t.Attribute("id") is not null)
            .GroupBy(t => (string)t.Attribute("id")!)
            .ToDictionary(g => g.Key, g => g.SelectMany(t => t.Elements("bitmap")).Select(b => (string?)b.Attribute("assetName")).OfType<string>().ToList());
        foreach (var column in document.Descendants("materialCellColumn"))
        {
            if ((int?)column.Attribute("width") is not { } width)
                continue;
            foreach (var cell in column.Elements("materialCell"))
                if ((string?)cell.Attribute("textureId") is { } texture && textures.TryGetValue(texture, out var assets))
                    foreach (var asset in assets)
                        _columnWidths.TryAdd(asset, width);
        }
    }

    private void ReadAssetsXml(HabEntry entry)
    {
        var document = XDocument.Parse(Encoding.Latin1.GetString(entry.Bytes));
        foreach (var element in document.Root?.Elements("asset") ?? [])
        {
            var name = (string?)element.Attribute("name");
            var source = (string?)element.Attribute("source");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(source))
                continue;
            _assets[name] = new AssetDefinition(source, (string?)element.Attribute("flipH") == "1", (string?)element.Attribute("flipV") == "1",
                (int?)element.Attribute("x") ?? 0, (int?)element.Attribute("y") ?? 0, null, null);
        }
    }

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps.Values.Concat(_sheets.Values).Distinct())
            bitmap?.Dispose();
    }
}
