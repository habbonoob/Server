using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Rooms.Copies;
using Habbo.Infrastructure;
using Habbo.Silo.Permissions;
using Habbo.Silo.Rooms.Copies;
using Habbo.Web;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Host;

/// <summary>Importing a copied room (tools/Habbo.RoomCopier) as a room of one's own, for those with the permission:
/// the copy is the body, and the answer says what was built and what this hotel lacks.</summary>
public static class RoomImportEndpoints
{
    public static void MapRoomImportEndpoints(this WebApplication app) =>
        app.MapPost("/api/rooms/import", ImportAsync);

    private static async Task<IResult> ImportAsync(
        RoomCopy copy, HttpContext context, IDbContextFactory<AppDbContext> dbFactory, RoomImporter importer,
        CancellationToken ct)
    {
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            if (await WebSession.UserAsync(context, db, ct) is not { } user)
                return Results.Unauthorized();
            if (!await PermissionQuery.HasAsync(db, user.Id, PermissionNames.RoomImport))
                return Results.Forbid();
            if (copy.Format is < 1 or > RoomCopy.CurrentFormat)
                return Results.BadRequest(new { error = $"This is a format {copy.Format} copy; the hotel reads formats 1 to {RoomCopy.CurrentFormat}." });

            var result = await importer.ImportAsync(user.Id, copy, ct);
            return result.RoomId is null ? Results.UnprocessableEntity(result) : Results.Ok(result);
        }
    }
}
