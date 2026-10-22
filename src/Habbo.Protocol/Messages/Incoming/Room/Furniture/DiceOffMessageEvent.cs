using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>1725 DiceOff — a double-click on a dice's off switch.</summary>
[Incoming(ClientMessages.DiceOffMessageEvent)]
public sealed partial record DiceOffMessageEvent(int ItemId);
