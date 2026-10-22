using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>313 BuildersClubPlaceWallItem — a Builders Club offer dragged onto a wall, at a wall location
/// (":w=3,4 l=12,30 r"). Confirmed as for the floor.</summary>
[Incoming(ClientMessages.BuildersClubPlaceWallItemMessageEvent)]
public sealed partial record BuildersClubPlaceWallItemMessageEvent(
    int PageId, int OfferId, string ExtraParam, string WallLocation, bool Confirmed);
