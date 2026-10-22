using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>RoomAdPurchaseInfoComposer — the rooms the player may promote</summary>
[Message(ServerMessages.RoomAdPurchaseInfoComposer)]
public sealed partial record RoomAdPurchaseInfoComposer(bool IsVip, IReadOnlyList<RoomAdRoomData> Rooms);
