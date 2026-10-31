using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Habbo.Bundles;
using Habbo.Camera;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Web;

/// <summary>Converts asset libraries into habbo-client's bundles (docs/asset-bundles.md) on first request, and keeps
/// them under the mirror's <c>.bundles</c> folder: a mirrored .hab, a component or class of the client binary, a
/// library from beside it. A bundle is made again when its source is newer, or the converter's version changes.</summary>
public sealed partial class AssetBundles(AssetMirror mirror, IOptions<BundleOptions> options, ILogger<AssetBundles> logger)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _converting = new(StringComparer.Ordinal);
    private readonly BundleOptions _options = options.Value;

    /// <summary>The bundle of a .hab the mirror holds (fetched into it first if it may be).</summary>
    public async Task<string?> MirrorAsync(string host, string path, CancellationToken ct)
    {
        if (!path.EndsWith(".hab", StringComparison.OrdinalIgnoreCase) || await mirror.FileAsync(host, path, ct) is not { } source)
            return null;
        return await ConvertAsync(Path.Combine("mirror", host, path), source,
            () => HabLibraryReader.Read(HabFile.Read(File.ReadAllBytes(source)), Path.GetFileNameWithoutExtension(source)), ct);
    }

    /// <summary>The bundle of one of the client binary's components.</summary>
    public Task<string?> ComponentAsync(string component, CancellationToken ct)
    {
        if (_options.ClientSourceDirectory is not { } directory || !LibraryName().IsMatch(component))
            return Task.FromResult<string?>(null);
        var manifest = Path.Combine(directory, "_assets", component + "_manifest.bin");
        return File.Exists(manifest)
            ? ConvertAsync(Path.Combine("client", "components", component), manifest, () => ClientComponentReader.Read(directory, component), ct)
            : Task.FromResult<string?>(null);
    }

    /// <summary>The bundle of what one class of the client binary embeds (`splash/PhotoSplashScreen`), or of the
    /// members named (comma-separated).</summary>
    public Task<string?> ClassAsync(string classPath, string? members, CancellationToken ct)
    {
        if (_options.ClientSourceDirectory is not { } directory || !ClassPath().IsMatch(classPath))
            return Task.FromResult<string?>(null);
        var only = members?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Order(StringComparer.Ordinal).ToList();
        if (only is not null && (only.Count == 0 || only.Count > MaxMembers || !only.All(LibraryName().IsMatch)))
            return Task.FromResult<string?>(null);
        var source = Path.Combine(directory, classPath + ".as");
        // one bundle per set of members, named after them
        var key = Path.Combine("client", "classes", only is null ? classPath : $"{classPath}@{string.Join('+', only)}");
        return File.Exists(source)
            ? ConvertAsync(key, source, () => ClientClassReader.Read(directory, classPath, only?.ToHashSet(StringComparer.Ordinal)), ct)
            : Task.FromResult<string?>(null);
    }

    /// <summary>The most members one class bundle may be asked for, so its name stays a file name.</summary>
    private const int MaxMembers = 16;

    /// <summary>The bundle of a library shipped beside the client.</summary>
    public Task<string?> LocalAsync(string library, CancellationToken ct)
    {
        if (_options.LocalIncludeDirectory is not { } directory || !LibraryName().IsMatch(library))
            return Task.FromResult<string?>(null);
        var source = Path.Combine(directory, library + ".hab");
        return File.Exists(source)
            ? ConvertAsync(Path.Combine("client", "local", library), source,
                () => HabLibraryReader.Read(HabFile.Read(File.ReadAllBytes(source)), library), ct)
            : Task.FromResult<string?>(null);
    }

    private async Task<string?> ConvertAsync(string key, string source, Func<BundleLibrary> read, CancellationToken ct)
    {
        if (mirror.Root is null)
            return null;
        var target = Path.Combine(mirror.Root, ".bundles", $"{key}.v{BundleWriter.Version}.r{BundleWriter.ConverterRevision}.hcb");
        if (File.Exists(target) && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(source))
            return target;

        // one conversion per bundle, however many ask for it at once
        var conversion = _converting.GetOrAdd(target, _ => new Lazy<Task<string?>>(() => Task.Run(() => Convert(target, read))));
        try
        {
            return await conversion.Value.WaitAsync(ct);
        }
        finally
        {
            if (conversion.Value.IsCompleted)
                _converting.TryRemove(target, out _);
        }
    }

    private string? Convert(string target, Func<BundleLibrary> read)
    {
        try
        {
            using var library = read();
            var bundle = BundleWriter.Write(library);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            var part = target + ".part";
            File.WriteAllBytes(part, bundle);
            File.Move(part, target, overwrite: true);
            logger.LogInformation("Bundled {Library}: {Sprites} images, {Documents} documents, {Bytes} bytes.",
                library.Name, library.Images.Count, library.Documents.Count, bundle.Length);
            return target;
        }
        catch (Exception exception) when (exception is InvalidDataException or NotSupportedException or IOException or System.Xml.XmlException or System.Text.Json.JsonException)
        {
            logger.LogWarning(exception, "Could not bundle {Target}.", target);
            return null;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$")]
    private static partial Regex LibraryName();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*(/[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex ClassPath();
}
