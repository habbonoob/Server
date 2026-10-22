namespace Habbo.Domain.Users;

/// <summary>One player's exception to a <see cref="Permission"/>: granted whatever their rank, or denied it.</summary>
public sealed class UserPermission
{
    public Guid UserId { get; set; }

    public required string PermissionName { get; set; }

    /// <summary>True: they may, whatever their rank. False: they may not, whatever their rank.</summary>
    public bool Granted { get; set; }
}
