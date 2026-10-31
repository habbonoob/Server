using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Habbo.Web;

/// <summary>Serves the asset mirror (<see cref="AssetMirror"/>: images.habbo.com's furniture, figure and flash
/// assets, its c_images, and the hotel's gamedata) under /mirror/{host}/{path}, so the clients load everything from
/// here instead of from Habbo. A file the mirror lacks is fetched into it on first use where it may be, and is a 404
/// otherwise. With <c>?format=bundle</c>, a .hab comes as habbo-client's bundle of it.</summary>
public static class MirrorEndpoints
{
    /// <summary>`?format=bundle`: the library converted for habbo-client (<see cref="AssetBundles"/>).</summary>
    public const string BundleFormat = "bundle";

    /// <summary>A bundle, revalidated every time it is used: its name stays the same when it is made again (a newer source,
    /// a newer converter), so a browser keeping it by guess would draw from the old one.</summary>
    public static IResult Bundle(HttpContext context, string file)
    {
        var written = File.GetLastWriteTimeUtc(file);
        context.Response.Headers.CacheControl = "no-cache";
        return Results.File(file, "application/octet-stream", lastModified: written,
            entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{written.Ticks:x}-{new FileInfo(file).Length:x}\""));
    }

    public static void MapMirrorEndpoints(this WebApplication app)
    {
        var types = new FileExtensionContentTypeProvider();
        types.Mappings[".swf"] = "application/x-shockwave-flash";

        app.MapGet("/mirror/{host}/{**path}", async (HttpContext context, string host, string path, string? format, AssetMirror mirror,
                AssetBundles bundles, CancellationToken ct) =>
            format == BundleFormat
                ? await bundles.MirrorAsync(host, path, ct) is { } bundle ? Bundle(context, bundle) : Results.NotFound()
                : await mirror.FileAsync(host, path, ct) is { } file
                    ? Results.File(file, types.TryGetContentType(file, out var type) ? type : "application/octet-stream",
                        enableRangeProcessing: true)
                    : Results.NotFound()).RequireRateLimiting(WebRateLimits.Mirror);
    }
}
