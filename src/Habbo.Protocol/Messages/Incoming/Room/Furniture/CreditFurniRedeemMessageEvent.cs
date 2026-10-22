using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>3810 CreditFurniRedeem — exchange a piece of credit furniture for what it is worth.
/// The client asks after its own confirmation dialog; the item is gone once the server agrees.</summary>
[Incoming(ClientMessages.CreditFurniRedeemMessageEvent)]
public sealed partial record CreditFurniRedeemMessageEvent(int ItemId);
