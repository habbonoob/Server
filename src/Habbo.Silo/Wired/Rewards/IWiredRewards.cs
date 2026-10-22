using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Rewards;

/// <summary>Hands out a give reward box's rewards, keeping to its limits, and tells the user what came of it.</summary>
public interface IWiredRewards
{
    Task RewardAsync(int boxId, RoomEntityStore.RoomEntity user, WiredRewardRules rules, Random random);

    /// <summary>The codes of the rewards the hotel has no such badge or product for.</summary>
    Task<IReadOnlyList<string>> MissingAsync(IReadOnlyList<WiredReward> rewards);
}
