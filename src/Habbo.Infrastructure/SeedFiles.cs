using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Habbo.Infrastructure;

/// <summary>Reads the seed content that ships as files next to the solution. Everything the
/// hotel starts life with — perks, navigator tabs, gestures, item definitions, rooms, gamedata —
/// lives there as data, so adding content never means editing C#.</summary>
public sealed class SeedFiles(IConfiguration configuration)
{
    /// <summary>Where the seed content lives, relative to the host's working directory
    /// unless the configured path is already absolute.</summary>
    private const string DirectoryKey = "Seed:Directory";
    private const string DefaultDirectory = "seed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public string Root => _root ??= ResolveRoot();
    private string? _root;

    /// <summary>An absolute configured path is taken as-is. A relative one is searched for from the
    /// binary's directory upwards, so the same setting works whether the host runs from bin/ during
    /// development or from a published folder.</summary>
    private string ResolveRoot()
    {
        var configured = configuration[DirectoryKey] ?? DefaultDirectory;
        if (Path.IsPathRooted(configured))
            return configured;

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, configured);
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }

    /// <summary>True when the seed directory is present; without it the seeder leaves the database alone.</summary>
    public bool Exists => Directory.Exists(Root);

    /// <summary>Reads and deserializes a JSON seed file. Returns null when the file isn't there,
    /// so a hotel can ship only the content it wants.</summary>
    public async Task<T?> ReadJsonAsync<T>(string relativePath, CancellationToken ct = default)
    {
        var path = Path.Combine(Root, relativePath);
        if (!File.Exists(path))
            return default;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }

    /// <summary>Reads a seed file verbatim — used for the gamedata the client downloads as text.</summary>
    public async Task<string?> ReadTextAsync(string relativePath, CancellationToken ct = default)
    {
        var path = Path.Combine(Root, relativePath);
        return File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;
    }

    /// <summary>Opens a seed file as a stream, or null when it is not there. Used for furnidata,
    /// which is tens of megabytes and is better streamed than read into a string first.</summary>
    public Stream? OpenRead(string relativePath)
    {
        var path = Path.Combine(Root, relativePath);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }
}
