using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>907 ActivityPointsMessageComposer — every currency's balance.</summary>
[Message(ServerMessages.ActivityPointsMessageComposer)]
public sealed partial record ActivityPointsMessageComposer(IReadOnlyList<ActivityPointData> Balances);
