using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>BuyHabbiconCollectionMessageEvent (September 2026) — buy every Habbicon of a collection at once, for the collection's price.</summary>
[Incoming(ClientMessages.BuyHabbiconCollectionMessageEvent)]
public sealed partial record BuyHabbiconCollectionMessageEvent(int CollectionId);
