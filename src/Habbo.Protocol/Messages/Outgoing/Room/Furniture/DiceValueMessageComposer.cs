using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>3351 DiceValueMessageComposer — a dice's face: -1 while it rolls, 0 when switched off,
/// otherwise the value thrown. The client sets it as the dice's state and clears its data.</summary>
[Message(ServerMessages.DiceValueMessageComposer)]
public sealed partial record DiceValueMessageComposer(int ItemId, int Value);
