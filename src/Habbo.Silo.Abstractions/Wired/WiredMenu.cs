namespace Habbo.Silo.Abstractions.Wired;

/// <summary>The permission levels the wired menu's settings give rights by, a bit each
/// (wiredmenu.settings.permission_level.N). The owner always has every right.</summary>
public static class WiredPermissionLevels
{
    public const int Everyone = 0;
    public const int UsersWithRights = 1;
    public const int GroupMembers = 2;
    public const int GroupAdmins = 3;
    public const int RoomOwner = 4;
}

/// <summary>A request for a page of the room's wired log.</summary>
[GenerateSerializer]
public sealed partial record WiredLogQuery(
    [property: Id(0)] int Page,
    [property: Id(1)] int PageSize,
    [property: Id(2)] int LevelFilter,
    [property: Id(3)] int SourceFilter,
    [property: Id(4)] string Query);

/// <summary>A variable a client holds, by the hash it cached it with.</summary>
[GenerateSerializer]
public sealed partial record KnownWiredVariable([property: Id(0)] string Id, [property: Id(1)] int Hash);
