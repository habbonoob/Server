using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>RoomAdPurchaseInitiatedEvent</summary>
[Incoming(ClientMessages.RoomAdPurchaseInitiatedEvent)]
public sealed partial record RoomAdPurchaseInitiatedEvent;
