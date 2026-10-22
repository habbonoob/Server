using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Habbo.Camera;

/// <summary>The images a photo names by url rather than by asset: posters' and photos' pictures, group badges, the
/// photo lab's effect images. Only this hotel's own urls are read (the client sends them, so anything else would
/// have the server fetch whatever a client asked): the asset mirror's and the photos' straight from their folders,
/// the rest (group badges) from the hotel's web server.</summary>
public sealed class ExternalImages(IConfiguration configuration, IHttpClientFactory http, PhotoStore photos, ILogger<ExternalImages> logger)
{
    private const string MirrorPath = "/mirror/";
    private const long MaxImageBytes = 4 * 1024 * 1024;

    /// <summary>The most images kept decoded; past it they are read again when next wanted.</summary>
    private const int MaxImages = 256;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly string _urlPrefix = (configuration["Web:UrlPrefix"] ?? "http://localhost:8080").TrimEnd('/');
    private readonly string? _mirror = CameraDirectories.Mirror(configuration);
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _images = new(StringComparer.Ordinal);

    public Task<SKBitmap?> GetAsync(string url)
    {
        if (_images.Count > MaxImages)
            _images.Clear();
        return _images.GetOrAdd(url, LoadAsync);
    }

    /// <summary>An image by its path on this hotel's web server.</summary>
    public Task<SKBitmap?> GetOwnAsync(string path) => GetAsync(_urlPrefix + path);

    private async Task<SKBitmap?> LoadAsync(string url)
    {
        try
        {
            var bytes = await ReadAsync(url);
            return bytes is null ? null : SKBitmap.Decode(bytes);
        }
        catch (Exception e) when (e is IOException or HttpRequestException or TaskCanceledException or UriFormatException)
        {
            logger.LogInformation("Could not read the photo's image {Url}: {Error}", url, e.Message);
            return null;
        }
    }

    private async Task<byte[]?> ReadAsync(string url)
    {
        // protocol relative, as the client's variables write some
        if (url.StartsWith("//", StringComparison.Ordinal))
            url = new Uri(_urlPrefix).Scheme + ":" + url;
        if (!url.StartsWith(_urlPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Not drawing {Url} in a photo: it is not this hotel's.", url);
            return null;
        }

        var path = Uri.UnescapeDataString(new Uri(url).AbsolutePath);
        if (path.StartsWith(MirrorPath, StringComparison.Ordinal))
            return _mirror is null ? null : ReadFile(_mirror, path[MirrorPath.Length..]);
        if (path.StartsWith(PhotoStore.PhotosPath, StringComparison.Ordinal))
            return photos.FindPhoto(path[PhotoStore.PhotosPath.Length..]) is { } photo ? await File.ReadAllBytesAsync(photo) : null;

        using var client = http.CreateClient(nameof(ExternalImages));
        client.Timeout = Timeout;
        client.MaxResponseContentBufferSize = MaxImageBytes;
        using var response = await client.GetAsync(url);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    /// <summary>A file under the mirror's folder, never outside it.</summary>
    private static byte[]? ReadFile(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) || !File.Exists(full) || new FileInfo(full).Length > MaxImageBytes)
            return null;
        return File.ReadAllBytes(full);
    }
}
