using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>1875 BuildersClubPlaceRoomItem — a Builders Club offer dragged from the warehouse onto the floor of the
/// room the player is in. Confirmed when the player has agreed to a placement warning (hiding the room) and the
/// client sends it again.</summary>
[Incoming(ClientMessages.BuildersClubPlaceRoomItemMessageEvent)]
public sealed partial record BuildersClubPlaceRoomItemMessageEvent(
    int PageId, int OfferId, string ExtraParam, int X, int Y, int Direction, bool Confirmed);
