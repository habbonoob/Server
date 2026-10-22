using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>121 SetActivatedBadges — what the player wears, slot by slot. The client always sends
/// all five slots, an empty code for an empty one.</summary>
[Incoming(ClientMessages.SetActivatedBadgesEvent)]
public sealed partial record SetActivatedBadgesEvent(
    int Slot1, string Badge1,
    int Slot2, string Badge2,
    int Slot3, string Badge3,
    int Slot4, string Badge4,
    int Slot5, string Badge5)
{
    public IEnumerable<(int Slot, string Code)> Slots =>
        [(Slot1, Badge1), (Slot2, Badge2), (Slot3, Badge3), (Slot4, Badge4), (Slot5, Badge5)];
}
