using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Vault;

/// <summary>IncomeRewardStatusMessageComposer — the player's earnings waiting to be claimed</summary>
[Message(ServerMessages.IncomeRewardStatusMessageComposer)]
public sealed partial record IncomeRewardStatusMessageComposer(IReadOnlyList<IncomeRewardData> Rewards);
