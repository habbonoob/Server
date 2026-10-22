using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Habbo.Domain.Groups;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Web;

/// <summary>Draws group badges from their codes, as Habbo's imager does: each layer's part image,
/// tinted in its colour, placed in its cell of a three-by-three grid, with the part's mask image
/// (its untinted detail) laid over it. The part images are Habbo's own, fetched from where the
/// client's image.library.badgepart.url points and kept once fetched.</summary>
public sealed partial class GroupBadgeImager(IDbContextFactory<AppDbContext> dbFactory, IHttpClientFactory http)
{
    /// <summary>A badge's size in pixels.</summary>
    private const int Size = 39;

    private const int GridCells = 3;
    private const string BasePrefix = "b";
    private const string HighSymbolPrefix = "t";
    private const int HighSymbolOffset = 100;

    private const string BadgePartUrlVariable = "image.library.badgepart.url";
    private const string ExternalVariables = "external_variables";
    private const string PartFilePrefix = "badgepart_";
    private const string PartFileExtension = ".png";
    private static readonly string[] SourceExtensions = [".gif", ".png"];
    private const string ProtocolRelative = "//";
    private const string DefaultScheme = "https:";

    [GeneratedRegex(@"([bst])(\d{2})(\d{2})(\d)")]
    private static partial Regex LayerPattern();

    /// <summary>A whole code: nothing but layers, and no more of them than a badge has (a base and four symbols,
    /// with room to spare). Anything else is not drawn, so it is not kept either.</summary>
    [GeneratedRegex(@"^(?:[bst]\d{5}){1,8}$")]
    private static partial Regex CodePattern();

    /// <summary>The most badges kept drawn; past it the cache starts again, so made-up codes cannot fill memory.</summary>
    private const int MaxCachedBadges = 10_000;

    /// <summary>How long the parts, colours and part address read from the database are used before they are read again.</summary>
    private static readonly TimeSpan CatalogLifetime = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, byte[]> _badges = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PngImage?> _parts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task<PartImage>> _fetching = new(StringComparer.Ordinal);
    private BadgeCatalog? _catalog;

    /// <summary>What drawing a badge needs from the database: every part, the badge colours, and where the part images are.</summary>
    private sealed record BadgeCatalog(List<GroupBadgePart> Parts, Dictionary<int, GroupColor> Colors, string? BaseUrl, DateTimeOffset ReadAt);

    /// <summary>A part's image (null when there is none to draw), and whether that is known for good: false when the
    /// fetch failed this time (the CDN erred or was slow) and may work next time.</summary>
    private readonly record struct PartImage(PngImage? Image, bool Known);

    /// <summary>The badge as a PNG, or null when the code names nothing the hotel has.</summary>
    public async Task<byte[]?> DrawAsync(string code, CancellationToken ct)
    {
        if (_badges.TryGetValue(code, out var cached))
            return cached;

        if (!CodePattern().IsMatch(code))
            return null;

        var layers = LayerPattern().Matches(code);
        var (parts, colors, baseUrl, _) = await CatalogAsync(ct);
        if (baseUrl is null)
            return null;

        var badge = new PngImage(Size, Size);
        // a part that couldn't be fetched this time leaves the badge drawn without it, and not kept so
        var complete = true;
        foreach (Match layer in layers)
        {
            var kind = layer.Groups[1].Value == BasePrefix ? GroupBadgePartKind.Base : GroupBadgePartKind.Symbol;
            var partId = int.Parse(layer.Groups[2].Value, CultureInfo.InvariantCulture)
                         + (layer.Groups[1].Value == HighSymbolPrefix ? HighSymbolOffset : 0);
            var colorId = int.Parse(layer.Groups[3].Value, CultureInfo.InvariantCulture);
            var position = int.Parse(layer.Groups[4].Value, CultureInfo.InvariantCulture);

            if (parts.FirstOrDefault(p => p.Kind == kind && p.Id == partId) is not { } part)
                continue;

            var tint = colors.TryGetValue(colorId, out var color) ? ParseHex(color.Hex) : White;
            var image = await PartAsync(baseUrl, part.FileName, ct);
            complete &= image.Known;
            if (image.Image is not null)
                Draw(badge, image.Image, position, tint);
            if (part.MaskFileName.Length == 0)
                continue;
            var mask = await PartAsync(baseUrl, part.MaskFileName, ct);
            complete &= mask.Known;
            if (mask.Image is not null)
                Draw(badge, mask.Image, position, null);
        }

        var bytes = badge.Encode();
        if (!complete)
            return bytes;
        if (_badges.Count >= MaxCachedBadges)
            _badges.Clear();
        _badges[code] = bytes;
        return bytes;
    }

    private async Task<BadgeCatalog> CatalogAsync(CancellationToken ct)
    {
        if (_catalog is { } known && DateTimeOffset.UtcNow - known.ReadAt < CatalogLifetime)
            return known;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var parts = await db.GroupBadgeParts.AsNoTracking().ToListAsync(ct);
        var colors = await db.GroupColors.AsNoTracking().Where(c => c.Kind == GroupColorKind.Badge).ToDictionaryAsync(c => c.Id, ct);
        return _catalog = new BadgeCatalog(parts, colors, await PartBaseUrlAsync(db, ct), DateTimeOffset.UtcNow);
    }

    /// <summary>Lays a part into its cell: left, centre or right, top, middle or bottom.</summary>
    private static void Draw(PngImage badge, PngImage part, int position, (byte R, byte G, byte B)? tint)
    {
        var column = position % GridCells;
        var row = position / GridCells;
        var offsetX = column switch { 0 => 0, 1 => (Size - part.Width) / 2, _ => Size - part.Width };
        var offsetY = row switch { 0 => 0, 1 => (Size - part.Height) / 2, _ => Size - part.Height };

        for (var y = 0; y < part.Height; y++)
        {
            for (var x = 0; x < part.Width; x++)
            {
                var (tx, ty) = (x + offsetX, y + offsetY);
                if (tx is < 0 or >= Size || ty is < 0 or >= Size)
                    continue;

                var pixel = part[x, y];
                if (pixel.A == 0)
                    continue;

                if (tint is { } t)
                    pixel = ((byte)(pixel.R * t.R / byte.MaxValue), (byte)(pixel.G * t.G / byte.MaxValue), (byte)(pixel.B * t.B / byte.MaxValue), pixel.A);

                badge[tx, ty] = Blend(badge[tx, ty], pixel);
            }
        }
    }

    /// <summary>Source-over alpha blending.</summary>
    private static (byte R, byte G, byte B, byte A) Blend((byte R, byte G, byte B, byte A) under, (byte R, byte G, byte B, byte A) over)
    {
        var a = over.A / 255f;
        var b = under.A / 255f * (1 - a);
        var alpha = a + b;
        if (alpha <= 0)
            return default;

        byte Mix(byte top, byte bottom) => (byte)Math.Round((top * a + bottom * b) / alpha);
        return (Mix(over.R, under.R), Mix(over.G, under.G), Mix(over.B, under.B), (byte)Math.Round(alpha * byte.MaxValue));
    }

    private async Task<PartImage> PartAsync(string baseUrl, string fileName, CancellationToken ct)
    {
        var name = SourceExtensions.Aggregate(fileName, (current, extension) => current.Replace(extension, string.Empty, StringComparison.OrdinalIgnoreCase));
        if (_parts.TryGetValue(name, out var cached))
            return new PartImage(cached, true);

        // one fetch of a part at a time, however many badges ask for it at once: a slow CDN would otherwise be asked
        // again by every request that came in while it was slow
        var fetch = _fetching.GetOrAdd(name, _ => FetchPartAsync(baseUrl, name));
        try
        {
            return await fetch.WaitAsync(ct);
        }
        finally
        {
            if (fetch.IsCompleted)
                _fetching.TryRemove(new KeyValuePair<string, Task<PartImage>>(name, fetch));
        }
    }

    private async Task<PartImage> FetchPartAsync(string baseUrl, string name)
    {
        PngImage? image = null;
        try
        {
            using var response = await http.CreateClient().GetAsync(baseUrl + PartFilePrefix + name + PartFileExtension);
            // a part Habbo no longer serves draws as nothing, as it does there
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
                image = PngImage.Decode(await response.Content.ReadAsByteArrayAsync());
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // the CDN erred or timed out: drawn as nothing this time, and asked for again next time
            return new PartImage(null, false);
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or IndexOutOfRangeException or ArgumentException)
        {
            // not a picture this can read: drawn as nothing, and not fetched again
        }

        _parts[name] = image;
        return new PartImage(image, true);
    }

    private static readonly (byte R, byte G, byte B) White = (byte.MaxValue, byte.MaxValue, byte.MaxValue);

    private static (byte R, byte G, byte B) ParseHex(string hex) =>
        hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)
            ? ((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb)
            : White;

    private static async Task<string?> PartBaseUrlAsync(AppDbContext db, CancellationToken ct)
    {
        var content = await db.WebGamedata.AsNoTracking().Where(g => g.Name == ExternalVariables).Select(g => g.Content).FirstOrDefaultAsync(ct);
        var line = content?.Split('\n').FirstOrDefault(l => l.StartsWith(BadgePartUrlVariable + "=", StringComparison.Ordinal));
        if (line is null)
            return null;

        var url = line[(BadgePartUrlVariable.Length + 1)..].Trim();
        return url.StartsWith(ProtocolRelative, StringComparison.Ordinal) ? DefaultScheme + url : url;
    }
}
