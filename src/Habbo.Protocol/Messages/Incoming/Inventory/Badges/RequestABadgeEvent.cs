using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Badges;

/// <summary>674 RequestABadge — ask for the badge a request code stands for.</summary>
[Incoming(ClientMessages.RequestABadgeEvent)]
public sealed partial record RequestABadgeEvent(string RequestCode);
