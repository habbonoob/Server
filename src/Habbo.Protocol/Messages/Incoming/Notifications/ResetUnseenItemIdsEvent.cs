using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Notifications;

/// <summary>1499 ResetUnseenItemIds — the player looked at these particular items.</summary>
[Incoming(ClientMessages.ResetUnseenItemIdsEvent)]
public sealed partial record ResetUnseenItemIdsEvent(int Category, IReadOnlyList<int> ItemIds);
