using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetCustomStackingHeightEvent — the stack helper's height in hundredths; -100 means on top of what is under it</summary>
[Incoming(ClientMessages.SetCustomStackingHeightEvent)]
public sealed partial record SetCustomStackingHeightEvent(int ItemId, int Height, bool? MultiWalk = null);
