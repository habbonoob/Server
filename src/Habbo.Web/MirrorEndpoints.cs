using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Habbo.Web;

/// <summary>Serves the asset mirror (<see cref="AssetMirror"/>: images.habbo.com's furniture, figure and flash
/// assets, its c_images, and the hotel's gamedata) under /mirror/{host}/{path}, so the clients load everything from
/// here instead of from Habbo. A file the mirror lacks is fetched into it on first use where it may be, and is a 404
/// otherwise.</summary>
public static class MirrorEndpoints
{
    public static void MapMirrorEndpoints(this WebApplication app)
    {
        var types = new FileExtensionContentTypeProvider();
        types.Mappings[".swf"] = "application/x-shockwave-flash";

        app.MapGet("/mirror/{host}/{**path}", async (string host, string path, AssetMirror mirror, CancellationToken ct) =>
            await mirror.FileAsync(host, path, ct) is { } file
                ? Results.File(file, types.TryGetContentType(file, out var type) ? type : "application/octet-stream",
                    enableRangeProcessing: true)
                : Results.NotFound()).RequireRateLimiting(WebRateLimits.Mirror);
    }
}
