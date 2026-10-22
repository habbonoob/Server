using System.Net.Mail;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Web;

/// <summary>The website's accounts: registering (with an invite code while the hotel is invite-only), logging in and
/// out, and who is logged in.</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", LoginAsync).RequireRateLimiting(WebRateLimits.Login);
        app.MapPost("/api/auth/register", RegisterAsync).RequireRateLimiting(WebRateLimits.Register);
        app.MapPost("/api/auth/logout", async (HttpContext context) =>
        {
            await WebSession.SignOutAsync(context);
            return Results.NoContent();
        });
        app.MapGet("/api/me", MeAsync);
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
    }

    /// <summary>A hash made once to check unknown usernames against; its password matters to nobody.</summary>
    private static string? _unknownUserHash;
    private const string UnknownUserPassword = "no account has this password";

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher<User> passwordHasher,
        IOptions<AuthOptions> options,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password)
            || request.Password.Length > options.Value.PasswordMaxLength)
            return Results.BadRequest();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), ct);
        // an unknown name is checked against a hash too, so it takes as long as a wrong password: the time an answer
        // takes would otherwise tell which usernames exist
        var unknown = new User { Username = string.Empty, PasswordHash = string.Empty };
        var hash = user?.PasswordHash ?? (_unknownUserHash ??= passwordHasher.HashPassword(unknown, UnknownUserPassword));
        if (passwordHasher.VerifyHashedPassword(user ?? unknown, hash, request.Password) == PasswordVerificationResult.Failed
            || user is null)
        {
            // One message for both failure modes: don't leak which usernames exist.
            return Results.Unauthorized();
        }

        await WebSession.SignInAsync(context, user);
        return Results.Ok(await AccountView.FromAsync(user, options.Value, db));
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext context,
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher<User> passwordHasher,
        INameRules nameRules,
        IOptions<AuthOptions> options,
        CancellationToken ct)
    {
        var rules = options.Value;
        var username = request.Username?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var code = request.InviteCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (password.Length < rules.PasswordMinLength || password.Length > rules.PasswordMaxLength)
            return Refused(RegisterError.Password);
        if (email.Length > User.EmailMaxLength || (email.Length > 0 && !MailAddress.TryCreate(email, out _)))
            return Refused(RegisterError.Email);
        var verdict = await nameRules.JudgeAsync(username);
        if (verdict != NameVerdict.Available)
            return Refused(verdict switch
            {
                NameVerdict.TooShort => RegisterError.NameTooShort,
                NameVerdict.TooLong => RegisterError.NameTooLong,
                NameVerdict.Taken => RegisterError.NameTaken,
                _ => RegisterError.NameInvalid,
            });

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new User { Username = username, PasswordHash = string.Empty, Email = email, Credits = rules.StartingCredits };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // registered by someone else between the check and now
            return Refused(RegisterError.NameTaken);
        }

        // the code is taken in the same transaction, and only while still unused: of two people using one code at
        // once, one gets in
        if (rules.InviteOnly && await db.InviteCodes.Where(i => i.Code == code && i.UsedById == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedById, user.Id).SetProperty(i => i.UsedAt, DateTimeOffset.UtcNow), ct) != 1)
        {
            await transaction.RollbackAsync(ct);
            return Refused(RegisterError.InviteCode);
        }

        await transaction.CommitAsync(ct);
        await WebSession.SignInAsync(context, user);
        return Results.Ok(await AccountView.FromAsync(user, rules, db));
    }

    private static async Task<IResult> MeAsync(
        HttpContext context, IDbContextFactory<AppDbContext> dbFactory, IOptions<AuthOptions> options, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await WebSession.UserAsync(context, db, ct) is { } user
            ? Results.Ok(await AccountView.FromAsync(user, options.Value, db))
            : Results.Unauthorized();
    }

    private static IResult Refused(string error) => Results.BadRequest(new { error });
}
