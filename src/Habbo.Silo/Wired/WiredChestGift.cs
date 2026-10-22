namespace Habbo.Silo.Wired;

/// <summary>What the rewarded user is shown, and what the transaction log records: the effect that gave, its reward
/// text, and whether the reward pops up by itself.</summary>
public sealed record WiredChestGift(int EffectItemId, string RewardText, bool OpenByDefault);
