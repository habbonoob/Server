using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>CustomStackingHeightUpdateMessageComposer — the stack helper's height, in hundredths</summary>
[Message(ServerMessages.CustomStackingHeightUpdateMessageComposer)]
public sealed partial record CustomStackingHeightUpdateMessageComposer(int ItemId, int Height);
