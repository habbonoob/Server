using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>339 ThrowDice — a double-click on a dice that is off or showing a value.</summary>
[Incoming(ClientMessages.ThrowDiceMessageEvent)]
public sealed partial record ThrowDiceMessageEvent(int ItemId);
