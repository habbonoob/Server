using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>ItemsChestContentsUpdatedMessageComposer — what left and entered a furni chest</summary>
[Message(ServerMessages.ItemsChestContentsUpdatedMessageComposer)]
public sealed partial record ItemsChestContentsUpdatedMessageComposer(int ChestId, IReadOnlyList<int> RemovedIds, IReadOnlyList<ChestStorageData> Added);
