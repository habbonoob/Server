using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Purse;

/// <summary>1287 CreditBalanceComposer — a string, the client parses it back to an int.</summary>
[Message(ServerMessages.CreditBalanceComposer)]
public sealed partial record CreditBalanceComposer(
    [property: WireFormat(WireType.IntegerString)] int Credits);
