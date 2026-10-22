using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetRoomAdPurchaseInfoEvent</summary>
[Incoming(ClientMessages.GetRoomAdPurchaseInfoEvent)]
public sealed partial record GetRoomAdPurchaseInfoEvent;
