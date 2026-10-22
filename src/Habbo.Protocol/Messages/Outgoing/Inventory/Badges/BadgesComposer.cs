using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Badges;

/// <summary>2315 BadgesComposer — the badge inventory, in fragments the client stitches together.</summary>
[Message(ServerMessages.BadgesComposer)]
public sealed partial record BadgesComposer(int TotalFragments, int FragmentNo, IReadOnlyList<BadgeData> Badges);
