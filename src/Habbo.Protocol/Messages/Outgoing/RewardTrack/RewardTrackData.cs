using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RewardTrack;

/// <summary>A reward track as the player sees it: its id and colour theme, their points, premium's price and
/// perks when the track sells it, whether they own premium, whether they have every free (and every premium) prize,
/// and its tasks and prizes with their own progress.</summary>
[WireStruct]
public sealed partial record RewardTrackData(
    string Id,
    string Theme,
    int Points,
    [property: WirePresence] RewardTrackPremiumData? PremiumConfig,
    bool Premium,
    bool Complete,
    bool PremiumComplete,
    IReadOnlyList<RewardTrackTaskData> Tasks,
    IReadOnlyList<RewardTrackPrizeData> Prizes);

/// <summary>What premium on a track gives and costs: task points multiplied by the boost, points at once, and its
/// price in diamonds and credits.</summary>
[WireStruct]
public sealed partial record RewardTrackPremiumData(
    double TaskPointsBoost,
    int InstantPoints,
    int CostDiamonds,
    int CostCredits);

/// <summary>A task of a track: what it counts (an action type, and a parameter narrowing it), the player's count,
/// whether only premium counts it, and its levels.</summary>
[WireStruct]
public sealed partial record RewardTrackTaskData(
    string Id,
    string ActionType,
    string Parameter,
    int ProgressCount,
    bool Premium,
    IReadOnlyList<RewardTrackTaskLevelData> Levels);

/// <summary>A level of a task: the count that reaches it, the points it gives, whether only premium earns it.</summary>
[WireStruct]
public sealed partial record RewardTrackTaskLevelData(
    int RequiredCount,
    int PointsReward,
    bool Premium);

/// <summary>A prize of a track: the points it unlocks at, what it is (a product type, the item, its extra
/// parameters, how many), whether it is on the premium side, and whether the player may claim it, and has.</summary>
[WireStruct]
public sealed partial record RewardTrackPrizeData(
    string Id,
    int RequiredPoints,
    [property: WireFormat(WireType.I16)] int ProductType,
    string RewardTypeId,
    string ExtraParams,
    int RewardAmount,
    bool Premium,
    bool Available,
    bool Claimed);
