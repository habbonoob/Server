using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>The kinds of avatar the user-type dialogs choose between (wiredfurni.params.usertype.N).</summary>
public static class WiredUserKinds
{
    public const int Habbo = 1;
    public const int Pet = 2;
    public const int Bot = 4;

    /// <summary>What kind of avatar the entity is. A room's entities are its players.</summary>
    public static int Of(RoomEntityStore.RoomEntity user) => Habbo;
}
