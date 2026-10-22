using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Recycler;

/// <summary>RecycleItemsMessageEvent — put inventory items through the Furni-Matic</summary>
[Incoming(ClientMessages.RecycleItemsMessageEvent)]
public sealed partial record RecycleItemsMessageEvent(IReadOnlyList<int> ItemIds);
