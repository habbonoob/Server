using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Vault;

/// <summary>IncomeRewardClaimResponseMessageComposer — how claiming earnings went</summary>
[Message(ServerMessages.IncomeRewardClaimResponseMessageComposer)]
public sealed partial record IncomeRewardClaimResponseMessageComposer(byte RewardCategory, bool Result);
