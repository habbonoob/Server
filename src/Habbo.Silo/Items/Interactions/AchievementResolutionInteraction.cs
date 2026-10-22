using Habbo.Silo.Achievements;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A resolution statue: its owner picks an achievement and a level of it to reach in time, and the statue is
/// engraved with the badge when they do. The room and <see cref="AchievementResolutions"/> run the challenge.</summary>
[FurniInteraction("furniture_achievement_resolution")]
public sealed class AchievementResolutionInteraction : FurniInteraction
{
    public override void Grant(FurniGrant grant) =>
        FurniDataStore.Attach(grant.Db, grant.Item, BadgeEngraving.Blank((int)AchievementResolutionState.NotStarted));
}
