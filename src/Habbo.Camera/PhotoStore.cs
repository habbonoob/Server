using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Camera;

/// <summary>The rendered photos, as files: "{id}.png" the photo and "{id}_small.png" the picture a photo furni shows
/// on the wall (the client asks for that name), served under /photos (stories.image_url_base); and the rooms'
/// navigator thumbnails, "thumbnails/{room id}.png", served under /navigator-thumbnail
/// (navigator.thumbnail.url_base).</summary>
public sealed class PhotoStore
{
    public const string PhotosPath = "/photos/";

    public const string ThumbnailsPath = "/navigator-thumbnail/";

    private const string ThumbnailFolder = "thumbnails";
    private const string RequestFolder = "requests";
    private const string SmallSuffix = "_small";
    private const string Extension = ".png";

    private readonly ILogger<PhotoStore> _logger;
    private readonly string _urlPrefix;

    public PhotoStore(IOptions<CameraOptions> options, IConfiguration configuration, ILogger<PhotoStore> logger)
    {
        _logger = logger;
        _urlPrefix = (configuration["Web:UrlPrefix"] ?? "http://localhost:8080").TrimEnd('/');
        Root = CameraDirectories.FindOrCreate(options.Value.PhotoDirectory, CameraDirectories.MirrorConfigured(configuration));
        Directory.CreateDirectory(Path.Combine(Root, ThumbnailFolder));
        logger.LogInformation("Keeping photos in {Root}.", Root);
    }

    public string Root { get; }

    /// <summary>The name the client is told a photo is at, relative to stories.image_url_base.</summary>
    public static string PhotoName(Guid id) => id.ToString("N") + Extension;

    /// <summary>Where a photo is on this hotel's web server.</summary>
    public string PhotoUrl(Guid id) => _urlPrefix + PhotosPath + PhotoName(id);

    public async Task SavePhotoAsync(Guid id, RenderedPhoto photo, CancellationToken ct)
    {
        await File.WriteAllBytesAsync(Path.Combine(Root, PhotoName(id)), photo.Png, ct);
        await File.WriteAllBytesAsync(Path.Combine(Root, id.ToString("N") + SmallSuffix + Extension), photo.SmallPng, ct);
    }

    public Task SaveThumbnailAsync(int roomId, byte[] png, CancellationToken ct) =>
        File.WriteAllBytesAsync(ThumbnailFile(roomId), png, ct);

    /// <summary>A photo's file by the name it is served under ("{id}.png" or "{id}_small.png"), when there is one.</summary>
    public string? FindPhoto(string name)
    {
        var id = name.EndsWith(SmallSuffix + Extension, StringComparison.Ordinal) ? name[..^(SmallSuffix.Length + Extension.Length)]
            : name.EndsWith(Extension, StringComparison.Ordinal) ? name[..^Extension.Length] : null;
        // only names the store gives out, so nothing outside its folder
        if (id is null || !Guid.TryParseExact(id, "N", out _))
            return null;
        var path = Path.Combine(Root, name);
        return File.Exists(path) ? path : null;
    }

    /// <summary>A room's thumbnail by the name it is served under ("{room id}.png"), when it has one.</summary>
    public string? FindThumbnail(string name)
    {
        if (!name.EndsWith(Extension, StringComparison.Ordinal)
            || !int.TryParse(name[..^Extension.Length], NumberStyles.None, CultureInfo.InvariantCulture, out var roomId))
            return null;
        var path = ThumbnailFile(roomId);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Keeps a render request as the client sent it, beside the photo, for when a photo comes out wrong.</summary>
    public async Task KeepRequestAsync(Guid id, string json, CancellationToken ct)
    {
        var folder = Path.Combine(Root, RequestFolder);
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, id.ToString("N") + ".json"), json, ct);
        _logger.LogDebug("Kept the render request of photo {Id}.", id);
    }

    private string ThumbnailFile(int roomId) =>
        Path.Combine(Root, ThumbnailFolder, roomId.ToString(CultureInfo.InvariantCulture) + Extension);
}
