using Microsoft.EntityFrameworkCore;

namespace Habbo.Infrastructure;

/// <summary>Whether a player has a permission, asked of the database: for code with no permission service
/// at hand, such as furniture interactions, which are made without services. The same rule: their own exception, else
/// their rank against the table's; a permission the table does not have is nobody's.</summary>
public static class PermissionQuery
{
    public static async Task<bool> HasAsync(AppDbContext db, Guid userId, string permission)
    {
        var own = await db.UserPermissions.AsNoTracking()
            .Where(e => e.UserId == userId && e.PermissionName == permission)
            .Select(e => (bool?)e.Granted)
            .FirstOrDefaultAsync();
        if (own is { } granted)
            return granted;

        return await db.Permissions.AsNoTracking()
            .AnyAsync(p => p.Name == permission && db.Users.Any(u => u.Id == userId && u.Rank >= p.MinimumRank));
    }
}
