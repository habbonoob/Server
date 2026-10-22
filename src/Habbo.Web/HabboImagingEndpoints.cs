using System.Text.RegularExpressions;
using Habbo.Camera.Avatars;
using Habbo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Web;

/// <summary>Habbo's imager, as the client and the website ask for it:
///  - /habbo-imaging/badge/{code}.gif, as the client's group.badge.url builds it. The client swaps the .gif for .png
///    itself; either is answered with a PNG.
///  - /habbo-imaging/avatarimage?figure=… or ?user=…, with Habbo's own parameters: a figure, or a player by name whose
///    figure it is; direction and head_direction (0-7), size (s small, m the room's, l the room's at twice the size),
///    headonly=1.</summary>
public static partial class HabboImagingEndpoints
{
    private const string PngContentType = "image/png";

    /// <summary>A figure's avatar only changes with the figure, which is in the address.</summary>
    private const string FigureCacheControl = "public, max-age=86400";

    /// <summary>A player's avatar changes when they change their look, under the same address.</summary>
    private const string UserCacheControl = "public, max-age=300";

    private const int MaxUsernameLength = 64;

    private const int MaxFigureLength = 512;

    /// <summary>The default way an avatar faces: towards the viewer's right, as Habbo's imager draws it.</summary>
    private const int DefaultDirection = 2;

    public static void MapHabboImagingEndpoints(this WebApplication app)
    {
        app.MapGet("/habbo-imaging/badge/{file}", async (string file, GroupBadgeImager imager, CancellationToken ct) =>
        {
            var code = Path.GetFileNameWithoutExtension(file);
            return await imager.DrawAsync(code, ct) is { } png ? Results.File(png, PngContentType) : Results.NotFound();
        }).RequireRateLimiting(WebRateLimits.Badges);

        app.MapGet("/habbo-imaging/avatarimage", async (HttpContext context, AvatarImager imager, IDbContextFactory<AppDbContext> dbFactory,
            CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var figure = query["figure"].ToString();
            var username = query["user"].ToString();
            if (figure.Length == 0 && username.Length is > 0 and <= MaxUsernameLength)
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (await db.Users.AsNoTracking().Where(u => u.Username.ToLower() == username.ToLower())
                        .Select(u => u.Figure).FirstOrDefaultAsync(ct) is not { } found)
                    return Results.NotFound();
                figure = found;
            }
            if (figure.Length is 0 or > MaxFigureLength || !FigurePattern().IsMatch(figure))
                return Results.BadRequest();
            var direction = Direction(query["direction"], DefaultDirection);
            var headDirection = Direction(query["head_direction"], direction);
            var (scale, zoom) = query["size"].ToString() switch
            {
                "s" => (AvatarImager.SmallScale, 1),
                "l" => (AvatarImager.LargeScale, 2),
                _ => (AvatarImager.LargeScale, 1),
            };
            var headOnly = query["headonly"] == "1";

            if (imager.Draw(figure, direction, headDirection, scale, headOnly, zoom) is not { } png)
                return Results.NotFound();
            context.Response.Headers.CacheControl = query["figure"].Count > 0 ? FigureCacheControl : UserCacheControl;
            return Results.File(png, PngContentType);
        }).RequireRateLimiting(WebRateLimits.Badges);
    }

    private static int Direction(string? text, int fallback) =>
        int.TryParse(text, out var direction) && direction is >= 0 and <= 7 ? direction : fallback;

    /// <summary>"hr-115-42.hd-190-1…": a set type, its set and its colours, for each part.</summary>
    [GeneratedRegex(@"^[a-z]{1,4}-\d{1,9}(-\d{1,9})*(\.[a-z]{1,4}-\d{1,9}(-\d{1,9})*)*$")]
    private static partial Regex FigurePattern();
}
