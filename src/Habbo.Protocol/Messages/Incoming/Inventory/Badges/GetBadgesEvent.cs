using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>1987 GetBadges — the badge inventory.</summary>
[Incoming(ClientMessages.GetBadgesEvent)]
public sealed partial record GetBadgesEvent;
