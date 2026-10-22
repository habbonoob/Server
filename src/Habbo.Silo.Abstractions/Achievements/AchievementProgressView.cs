using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Achievements;

/// <summary>One achievement and one player's progress through it, already resolved against the
/// level they are on. The scores are absolute totals counted from zero, as the client reads them.
/// </summary>
[GenerateSerializer]
public partial record AchievementProgressView(
    [property: Id(0)] int AchievementId,
    [property: Id(1)] int Level,
    [property: Id(2)] string BadgeId,
    [property: Id(3)] int ScoreAtStartOfLevel,
    [property: Id(4)] int ScoreLimit,
    [property: Id(5)] int LevelRewardPoints,
    [property: Id(6)] int LevelRewardPointType,
    [property: Id(7)] int CurrentPoints,
    [property: Id(8)] bool FinalLevel,
    [property: Id(9)] string Category,
    [property: Id(10)] string SubCategory,
    [property: Id(11)] int LevelCount,
    [property: Id(12)] int DisplayMethod,
    [property: Id(13)] AchievementState State);
