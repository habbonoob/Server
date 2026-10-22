using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>One change to the friend list.
///
/// The tag decides what follows it: a removal carries only the id, while an addition or an
/// update carries the whole friend. The fields are declared in wire order and gated on the tag,
/// so each kind writes exactly its own and nothing else — the same shape as an item's stuff
/// data, for the same reason.</summary>
[WireStruct]
public sealed partial record FriendListChangeData(
    int Change,
    [property: WireWhen("IsRemoval")] int? RemovedFriendId,
    [property: WireWhen("CarriesFriend")] FriendData? Friend)
{
    /// <summary>A friend who is gone. The client reads the id on its own.</summary>
    public const int Removed = -1;

    /// <summary>A friend who was already there and has changed — come online, changed looks.</summary>
    public const int Updated = 0;

    /// <summary>A friend the client has not seen before.</summary>
    public const int Added = 1;

    public bool IsRemoval => Change == Removed;

    public bool CarriesFriend => Change is Updated or Added;

    public static FriendListChangeData Remove(int friendId) => new(Removed, friendId, null);

    public static FriendListChangeData Update(FriendData friend) => new(Updated, null, friend);

    public static FriendListChangeData Add(FriendData friend) => new(Added, null, friend);
}
