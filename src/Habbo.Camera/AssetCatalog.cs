using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Camera;

/// <summary>Finds the asset a photo's sprite names, in the libraries the client drew it from: the asset mirror's
/// (images.habbo.com: furni under dcr/hof_furni/{revision}, the avatar's, effects' and pets' under
/// gordon/{flash assets}) and the client's own (the room's planes, which it carries rather than downloads).
///
/// A name says its library the way the client built it:
///  - an avatar part is "{size}_{action}_{part type}_{part id}_{direction}_{frame}", in the library figuremap.xml
///    gives the part; an effect's is "{size}_{action}_fx{effect id}_…", in the one effectmap.xml gives the effect;
///  - a furni's or pet's starts with its library's name ("chair_polyfon_64_a_2_0" is chair_polyfon's).</summary>
public sealed partial class AssetCatalog
{
    /// <summary>The libraries every avatar is drawn from besides figuremap's: its body, its hand items, its
    /// effects' shared parts.</summary>
    private static readonly string[] AvatarBaseLibraries = ["hh_human_body", "hh_human_item", "hh_human_fx", "hh_human_face"];

    /// <summary>How many libraries are kept read; past it they are read again when next wanted.</summary>
    private const int MaxLibraries = 384;

    private readonly ILogger<AssetCatalog> _logger;
    private readonly Lazy<AssetIndex> _index;
    private readonly ConcurrentDictionary<string, Lazy<HabLibrary?>> _libraries = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string?> _resolved = new(StringComparer.Ordinal);

    public AssetCatalog(IOptions<CameraOptions> options, IConfiguration configuration, ILogger<AssetCatalog> logger)
    {
        _logger = logger;
        var local = options.Value.LocalLibraryDirectory;
        _index = new Lazy<AssetIndex>(() => BuildIndex(CameraDirectories.Mirror(configuration), CameraDirectories.Find(local)));
    }

    /// <summary>The asset by its name, from whichever library has it; null when none does.</summary>
    public LibraryAsset? Find(string name)
    {
        var path = _resolved.GetOrAdd(name, Locate);
        return path is null ? null : Library(path)?.Get(name);
    }

    /// <summary>The width of a plane texture column drawn from the asset, from the library that has it.</summary>
    public int? ColumnWidth(string name)
    {
        var path = _resolved.GetOrAdd(name, Locate);
        return path is null ? null : Library(path)?.ColumnWidth(name);
    }

    /// <summary>A data asset (a pet's palette) from the library that has the asset <paramref name="beside"/>.</summary>
    public byte[]? FindBytes(string beside, string name)
    {
        var path = _resolved.GetOrAdd(beside, Locate);
        return path is null ? null : Library(path)?.GetBytes(name);
    }

    private string? Locate(string name)
    {
        var index = _index.Value;
        foreach (var path in Candidates(index, name))
            if (Library(path)?.Has(name) == true)
                return path;
        _logger.LogDebug("No library has the asset {Name}.", name);
        return null;
    }

    private static IEnumerable<string> Candidates(AssetIndex index, string name)
    {
        foreach (var path in index.Local)
            yield return path;

        if (AvatarAssetPattern().Match(name) is { Success: true } avatar)
        {
            var type = avatar.Groups["type"].Value;
            var id = avatar.Groups["id"].Value;
            if (EffectTypePattern().Match(type) is { Success: true } effect
                && index.Effects.TryGetValue(effect.Groups["id"].Value, out var effectLibrary)
                && index.Gordon.TryGetValue(effectLibrary, out var effectPath))
                yield return effectPath;
            if (index.Parts.TryGetValue($"{type}:{id}", out var partLibrary) && index.Gordon.TryGetValue(partLibrary, out var partPath))
                yield return partPath;
            if (index.PartsById.TryGetValue(id, out var siblings))
                foreach (var sibling in siblings)
                    if (index.Gordon.TryGetValue(sibling, out var siblingPath))
                        yield return siblingPath;
            foreach (var library in AvatarBaseLibraries)
                if (index.Gordon.TryGetValue(library, out var basePath))
                    yield return basePath;
        }

        // a room object's library is the longest start of the name, at an underscore, that names one
        var parts = name.Split('_');
        for (var count = parts.Length - 1; count >= 1; count--)
        {
            var prefix = string.Join('_', parts, 0, count);
            if (index.Furni.TryGetValue(prefix, out var furni))
                yield return furni;
            if (index.Gordon.TryGetValue(prefix, out var gordon))
                yield return gordon;
        }
    }

    private HabLibrary? Library(string path)
    {
        if (_libraries.Count > MaxLibraries)
            _libraries.Clear();
        return _libraries.GetOrAdd(path, p => new Lazy<HabLibrary?>(() => Read(p))).Value;
    }

    private HabLibrary? Read(string path)
    {
        try
        {
            return HabLibrary.Read(File.ReadAllBytes(path));
        }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or System.Xml.XmlException)
        {
            _logger.LogWarning(e, "Could not read the asset library {Path}.", path);
            return null;
        }
    }

    private AssetIndex BuildIndex(string? mirror, string? local)
    {
        var index = new AssetIndex();
        if (local is not null)
            index.Local.AddRange(Directory.EnumerateFiles(local, "*.hab").Order(StringComparer.Ordinal));

        var host = mirror is null ? null : Path.Combine(mirror, "images.habbo.com");
        if (host is not null && Directory.Exists(Path.Combine(host, "dcr", "hof_furni")))
        {
            // a furni is kept under each revision it had; the newest is the one drawn
            foreach (var revision in Directory.EnumerateDirectories(Path.Combine(host, "dcr", "hof_furni"))
                         .OrderByDescending(d => long.TryParse(Path.GetFileName(d), NumberStyles.None, CultureInfo.InvariantCulture, out var r) ? r : -1))
                foreach (var file in Directory.EnumerateFiles(revision, "*.hab"))
                    index.Furni.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }

        if (host is not null && Directory.Exists(Path.Combine(host, "gordon")))
        {
            // the newest flash assets first: the one with the client's figuremap
            foreach (var folder in Directory.EnumerateDirectories(Path.Combine(host, "gordon")).OrderByDescending(d => d, StringComparer.Ordinal))
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.hab"))
                    index.Gordon.TryAdd(Path.GetFileNameWithoutExtension(file), file);
                ReadFigureMap(Path.Combine(folder, "figuremap.xml"), index);
                ReadEffectMap(Path.Combine(folder, "effectmap.xml"), index);
            }
        }

        _logger.LogInformation("Camera assets: {Furni} furni, {Gordon} other libraries, {Local} of the client's own, {Parts} avatar parts, {Effects} effects.",
            index.Furni.Count, index.Gordon.Count, index.Local.Count, index.Parts.Count, index.Effects.Count);
        return index;
    }

    private static void ReadFigureMap(string path, AssetIndex index)
    {
        if (!File.Exists(path))
            return;
        foreach (var library in XDocument.Load(path).Root?.Elements("lib") ?? [])
        {
            var id = (string?)library.Attribute("id");
            if (string.IsNullOrEmpty(id))
                continue;
            foreach (var part in library.Elements("part"))
            {
                var partId = (string?)part.Attribute("id") ?? "";
                index.Parts.TryAdd($"{(string?)part.Attribute("type")}:{partId}", id);
                var siblings = index.PartsById.TryGetValue(partId, out var found) ? found : index.PartsById[partId] = [];
                if (!siblings.Contains(id))
                    siblings.Add(id);
            }
        }
    }

    private static void ReadEffectMap(string path, AssetIndex index)
    {
        if (!File.Exists(path))
            return;
        foreach (var effect in XDocument.Load(path).Root?.Elements("effect") ?? [])
            if ((string?)effect.Attribute("id") is { } id && (string?)effect.Attribute("lib") is { Length: > 0 } library)
                index.Effects.TryAdd(id, library);
    }

    [GeneratedRegex(@"^(?:h|sh)_[a-z]+_(?<type>[a-z]+\d*)_(?<id>\d+)_")]
    private static partial Regex AvatarAssetPattern();

    [GeneratedRegex(@"^fx(?<id>\d+)$")]
    private static partial Regex EffectTypePattern();
}
