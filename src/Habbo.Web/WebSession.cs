using System.Security.Claims;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Web;

/// <summary>The website's login: a cookie naming the account, signed by ASP.NET's data protection. The hotel itself is
/// entered with a short-lived SSO ticket the website hands out to whoever holds the cookie.</summary>
public static class WebSession
{
    public const string CookieName = "habbo_session";

    public static Task SignInAsync(HttpContext context, User user) =>
        context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
                CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });

    public static Task SignOutAsync(HttpContext context) =>
        context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>The logged-in account, or null for nobody (or an account since deleted).</summary>
    public static async Task<User?> UserAsync(HttpContext context, AppDbContext db, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct) : null;
    }
}
