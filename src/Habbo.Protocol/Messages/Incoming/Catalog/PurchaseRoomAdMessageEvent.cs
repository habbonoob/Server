using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseRoomAdMessageEvent — promote a room: buy its event, or more time for the one it has</summary>
[Incoming(ClientMessages.PurchaseRoomAdMessageEvent)]
public sealed partial record PurchaseRoomAdMessageEvent(int PageId, int OfferId, int FlatId, string Name, bool Extended, string Description, int CategoryId);
