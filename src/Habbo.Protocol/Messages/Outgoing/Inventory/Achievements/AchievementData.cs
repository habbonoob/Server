using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;

/// <summary>One achievement as the client reads it. The two score fields are absolute totals
/// counted from zero: the client subtracts <see cref="ScoreAtStartOfLevel"/> from each itself
/// to draw the bar for the level being worked on.</summary>
[WireStruct]
public sealed partial record AchievementData(
    int AchievementId,
    int Level,
    string BadgeId,
    int ScoreAtStartOfLevel,
    int ScoreLimit,
    int LevelRewardPoints,
    int LevelRewardPointType,
    int CurrentPoints,
    bool FinalLevel,
    string Category,
    string SubCategory,
    int LevelCount,
    int DisplayMethod,
    [property: WireFormat(WireType.I16)] short State)
{
    /// <summary>Where a player stands on an achievement, as the dialog, the login push and a level-up all send it.</summary>
    public static AchievementData From(AchievementProgressView progress) => new(
        progress.AchievementId,
        progress.Level,
        progress.BadgeId,
        progress.ScoreAtStartOfLevel,
        progress.ScoreLimit,
        progress.LevelRewardPoints,
        progress.LevelRewardPointType,
        progress.CurrentPoints,
        progress.FinalLevel,
        progress.Category,
        progress.SubCategory,
        progress.LevelCount,
        progress.DisplayMethod,
        (short)progress.State);
}
