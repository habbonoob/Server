namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>How much control a user has in a room, as the client numbers it (YouAreController,
/// the avatar's "flatctrl" status): none, rights, a group's rights, a group's admin, the owner
/// (the room's own level), and staff.</summary>
public static class RoomControllerLevels
{
    public const int None = 0;
    public const int Rights = 1;
    public const int GroupRights = 2;
    public const int GroupAdmin = 3;
    public const int Staff = 5;
}

/// <summary>A user with rights in a room, for its settings.</summary>
[GenerateSerializer]
public sealed partial record FlatController([property: Id(0)] int AccountId, [property: Id(1)] string Username);

/// <summary>Rights given: the room, and who now has them.</summary>
[GenerateSerializer]
public sealed partial record FlatControllerAdded([property: Id(0)] int RoomId, [property: Id(1)] FlatController Controller);

/// <summary>Rights taken back: the room, and from whom (by account id).</summary>
[GenerateSerializer]
public sealed partial record RightsRemoved([property: Id(0)] int RoomId, [property: Id(1)] IReadOnlyList<int> AccountIds);
