using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>The room struct the client reads in search results and GetGuestRoomResult.
/// After the tags it reads one bitmask: bits 1, 2 and 4 each gate a payload, while bits 8,
/// 16 and 32 are booleans carried by the mask itself.</summary>
[WireStruct]
public sealed partial record GuestRoomData(
    int RoomId,
    string Name,
    int OwnerAccountId,
    string OwnerName,
    int DoorMode,
    int UserCount,
    int MaxUsers,
    string Description,
    int TradeMode,
    int Score,
    int Ranking,
    int CategoryId,
    IReadOnlyList<string> Tags,

    [property: WireFlag(0x01)] string? OfficialPicRef,
    [property: WireFlag(0x02)] HabboGroupInfo? Group,
    [property: WireFlag(0x04)] RoomAdInfo? Ad,
    [property: WireFlag(0x08)] bool ShowOwner,
    [property: WireFlag(0x10)] bool AllowPets,
    [property: WireFlag(0x20)] bool DisplayRoomEntryAd)
{
    /// <summary>Projects the room the grains hand us. The room entry ad is the advertisement
    /// package's, so its bit stays clear.</summary>
    public static GuestRoomData From(RoomInfoData room) => new(
        room.RoomId,
        room.Name,
        room.OwnerAccountId,
        room.OwnerName,
        room.DoorMode,
        room.UserCount,
        room.MaxUsers,
        room.Description,
        room.TradeMode,
        room.Score,
        room.Ranking,
        room.CategoryId,
        room.Tags,
        string.IsNullOrEmpty(room.OfficialPicRef) ? null : room.OfficialPicRef,
        room.Group is { } group ? new HabboGroupInfo(group.GroupId, group.Name, group.BadgeCode) : null,
        room.Ad is { } ad ? new RoomAdInfo(ad.Name, ad.Description, ad.ExpiresInMinutes) : null,
        room.ShowOwner,
        room.AllowPets,
        // Habbo sets it on every room it lists (231 of 231 in a navigator capture, owned, public and group rooms)
        DisplayRoomEntryAd: true);
}
