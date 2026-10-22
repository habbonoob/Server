using Habbo.Domain.Users;

namespace Habbo.Silo.Permissions;

/// <summary>The permissions table and the players' exceptions to it, as read at one time.</summary>
internal sealed record PermissionSnapshot(
    IReadOnlyDictionary<string, Permission> Permissions,
    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, bool>> Exceptions)
{
    public static PermissionSnapshot Empty { get; } =
        new(new Dictionary<string, Permission>(), new Dictionary<Guid, IReadOnlyDictionary<string, bool>>());
}
