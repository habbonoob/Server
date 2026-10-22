using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>2151 FurniListAddOrUpdate — items the client merges into the inventory it already
/// holds, keyed by item id. Used after a purchase so the whole inventory need not be resent.</summary>
[Message(ServerMessages.FurniListAddOrUpdateComposer)]
public sealed partial record FurniListAddOrUpdateComposer(IReadOnlyList<InventoryFurniData> Items);
