namespace Habbo.Silo.Wired;

/// <summary>What the rewarded user is shown, and what the transaction log records: the effect that gave (by its
/// database id, as the log outlives the room's ids), its reward text, and whether the reward pops up by itself.</summary>
public sealed record WiredChestGift(int EffectItemId, string RewardText, bool OpenByDefault);
