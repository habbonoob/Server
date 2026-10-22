using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Vault;

/// <summary>IncomeRewardClaimMessageEvent — claim one category of earnings, or all of them</summary>
[Incoming(ClientMessages.IncomeRewardClaimMessageEvent)]
public sealed partial record IncomeRewardClaimMessageEvent(byte Category);
