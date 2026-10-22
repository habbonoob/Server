using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Vault;

/// <summary>IncomeRewardNotificationMessageComposer — new earnings are waiting in a category</summary>
[Message(ServerMessages.IncomeRewardNotificationMessageComposer)]
public sealed partial record IncomeRewardNotificationMessageComposer(byte RewardCategory);
