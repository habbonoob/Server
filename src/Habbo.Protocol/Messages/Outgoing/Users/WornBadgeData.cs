using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>A badge a user wears; since September 2026 with how many own it and how rare that makes it.</summary>
[WireStruct]
public sealed partial record WornBadgeData(
    int Slot,
    string Code,
    [property: WireSince(ClientReleases.September2026)] int Owners = 0,
    [property: WireSince(ClientReleases.September2026)] BadgeRarity Rarity = BadgeRarity.Common);
