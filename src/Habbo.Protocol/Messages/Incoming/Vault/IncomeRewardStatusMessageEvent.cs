using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Vault;

/// <summary>IncomeRewardStatusMessageEvent — the player's earnings waiting to be claimed</summary>
[Incoming(ClientMessages.IncomeRewardStatusMessageEvent)]
public sealed partial record IncomeRewardStatusMessageEvent;
