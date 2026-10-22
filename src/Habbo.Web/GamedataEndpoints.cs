using System.Security.Cryptography;
using System.Text;
using Habbo.Domain.Web;
using Habbo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Habbo.Web;

/// <summary>Serves the client's static data (external_variables, external_texts, furnidata, productdata)
/// from the database, mirroring how HabboAir stores gamedata. Import existing rows into web_gamedata freely.</summary>
public static class GamedataEndpoints
{
    public static void MapGamedataEndpoints(this WebApplication app, IConfiguration configuration)
    {
        var urlPrefix = configuration["Web:UrlPrefix"] ?? "http://localhost:8080";

        app.MapGet("/gamedata/hashes", async (IDbContextFactory<AppDbContext> dbFactory, CancellationToken ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var entries = await db.WebGamedata.AsNoTracking().ToListAsync(ct);

            var hashes = entries.Select(e => new
            {
                name = e.Name,
                // Habbo's layout: the client appends "/{hash}" to this url itself
                // (CoreLocalizationManager: getExternalTextsUrl() + "/" + getExternalTextsHash())
                url = $"{urlPrefix}/gamedata/{e.Name}/1",
                hash = e.Hash,
            });

            return Results.Ok(new { hashes });
        });

        // "/gamedata/{name}/1" as external_variables names it, and "/gamedata/{name}/1/{hash}" as the
        // client builds it from the hashes manifest: the same content either way
        app.MapGet("/gamedata/{name}/{version}/{hash}", (
            string name, string version, string hash,
            IDbContextFactory<AppDbContext> dbFactory,
            CancellationToken ct) => ServeAsync(name, dbFactory, ct));

        app.MapGet("/gamedata/{name}/{hash}", (
            string name, string hash,
            IDbContextFactory<AppDbContext> dbFactory,
            CancellationToken ct) => ServeAsync(name, dbFactory, ct));
    }

    private static async Task<IResult> ServeAsync(string name, IDbContextFactory<AppDbContext> dbFactory, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = await db.WebGamedata.AsNoTracking().FirstOrDefaultAsync(e => e.Name == name, ct);
        if (entry is null)
            return Results.NotFound();

        // The hash in the path is a cache-buster, not a version to serve: external_variables
        // points the client at a fixed "1" URL, and the hashes manifest hands out the current
        // one. Answering an older hash with 404 would leave the client without its furniture.
        return Results.Text(entry.Content, "text/plain", Encoding.UTF8);
    }

    /// <summary>SHA-256 of the content, lowercase hex — used in URLs and the hashes manifest.</summary>
    public static string ComputeHash(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
