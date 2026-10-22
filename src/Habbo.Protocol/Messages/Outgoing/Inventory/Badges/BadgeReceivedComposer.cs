using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>3284 BadgeReceivedComposer — a new badge landed in the inventory; since September 2026 with how many own it
/// and how rare that makes it.</summary>
[Message(ServerMessages.BadgeReceivedComposer)]
public sealed partial record BadgeReceivedComposer(
    int BadgeId,
    string BadgeCode,
    [property: WireSince(ClientReleases.September2026)] int Owners,
    [property: WireSince(ClientReleases.September2026)] BadgeRarity Rarity);
