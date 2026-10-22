using System.Security.Cryptography;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Web;

/// <summary>Invite codes, for staff of the configured rank: the list, a new one, and withdrawing one not used yet.</summary>
public static class InviteEndpoints
{
    /// <summary>No 0/O or 1/I: codes are read aloud and typed off phones.</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static void MapInviteEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/invites", ListAsync);
        app.MapPost("/api/admin/invites", CreateAsync);
        app.MapDelete("/api/admin/invites/{code}", WithdrawAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IOptions<AuthOptions> options, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!await MayManageAsync(context, db, options.Value, ct))
            return Results.Forbid();

        var invites = await db.InviteCodes.AsNoTracking().OrderByDescending(i => i.CreatedAt)
            .Select(i => new InviteView(i.Code, i.Note, i.CreatedAt, i.UsedBy == null ? null : i.UsedBy.Username, i.UsedAt))
            .ToListAsync(ct);
        return Results.Ok(invites);
    }

    private static async Task<IResult> CreateAsync(
        NewInvite request, HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IOptions<AuthOptions> options,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await WebSession.UserAsync(context, db, ct) is not { } user || user.Rank < options.Value.InviteManagerRank)
            return Results.Forbid();

        var note = (request.Note ?? string.Empty).Trim();
        if (note.Length > InviteCode.NoteMaxLength)
            return Results.BadRequest(new { error = "note" });

        var invite = new InviteCode
        {
            Code = RandomNumberGenerator.GetString(Alphabet, InviteCode.CodeLength), Note = note, CreatedById = user.Id,
        };
        db.InviteCodes.Add(invite);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new InviteView(invite.Code, invite.Note, invite.CreatedAt, null, null));
    }

    private static async Task<IResult> WithdrawAsync(
        string code, HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IOptions<AuthOptions> options,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!await MayManageAsync(context, db, options.Value, ct))
            return Results.Forbid();

        // a used code stays: it records who let the account in
        var removed = await db.InviteCodes.Where(i => i.Code == code && i.UsedById == null).ExecuteDeleteAsync(ct);
        return removed == 1 ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<bool> MayManageAsync(HttpContext context, AppDbContext db, AuthOptions options, CancellationToken ct) =>
        await WebSession.UserAsync(context, db, ct) is { } user && user.Rank >= options.InviteManagerRank;
}
