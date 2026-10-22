using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>3652 FurniListComposer — the fragmented inventory stream.</summary>
[Message(ServerMessages.FurniListComposer)]
public sealed partial record FurniListComposer(
    int TotalFragments, int FragmentNumber, IReadOnlyList<InventoryFurniData> Items);
