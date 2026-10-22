namespace Habbo.Domain.Users;

/// <summary>What a player is allowed to do, as the client's SecurityLevelEnum names it. The
/// levels are ordered and everything compares against them that way — a perk or a catalog page
/// asks for a minimum and anyone at or above it gets in.</summary>
public enum SecurityLevel
{
    None = 0,

    /// <summary>The client has a level between None and Partner whose name its own source no
    /// longer carries. Nothing reads it by name, only by order, so it is named for where it sits.
    /// </summary>
    AboveNone = 1,

    Partner = 2,
    BusinessPartner = 3,
    Employee = 4,
    Moderator = 5,
    PlayerSupport = 6,
    Community = 7,
    Administrator = 8,
    SuperUser = 9,
}
