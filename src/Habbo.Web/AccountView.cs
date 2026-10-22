using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Permissions;

namespace Habbo.Web;

/// <summary>The logged-in account as the website shows it, with the staff pages it may open.</summary>
public sealed record AccountView(string Username, string Figure, string Gender, string Motto, int Credits,
    DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt, bool CanManageInvites, bool CanImportRooms)
{
    public static async Task<AccountView> FromAsync(User user, AuthOptions options, AppDbContext db) => new(
        user.Username, user.Figure, user.Gender.ToLetter(), user.Motto, user.Credits, user.CreatedAt, user.LastLoginAt,
        user.Rank >= options.InviteManagerRank,
        await PermissionQuery.HasAsync(db, user.Id, PermissionNames.RoomImport));
}
