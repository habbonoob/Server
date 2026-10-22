using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests.Storage;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>ItemsChestContentsChunkMessageComposer — part of what a furni chest holds</summary>
[Message(ServerMessages.ItemsChestContentsChunkMessageComposer)]
public sealed partial record ItemsChestContentsChunkMessageComposer(int ChestId, int TotalFragments, int FragmentNo, IReadOnlyList<ChestStorageData> Storages);
