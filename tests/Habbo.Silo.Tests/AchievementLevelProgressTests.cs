using Habbo.Domain.Achievements;
using Habbo.Silo.Achievements;

namespace Habbo.Silo.Tests;

public class AchievementLevelProgressTests
{
    private static List<AchievementLevel> Levels(params int[] requiredProgress) =>
        requiredProgress
            .Select((required, index) => new AchievementLevel
            {
                Level = index + 1,
                BadgeCode = $"ACH_Test{index + 1}",
                RequiredProgress = required,
                RewardPoints = index + 1,
            })
            .ToList();

    [Fact]
    public void AFreshAchievementShowsItsFirstLevelFromZero()
    {
        var level = AchievementLevelProgress.Resolve(Levels(1, 5, 25), levelsFinished: 0, levelCount: 3);

        Assert.Equal(1, level.Level);
        Assert.Equal(0, level.StartOfLevel);
        Assert.Equal(1, level.ScoreLimit);
        Assert.False(level.IsFinal);
    }

    [Fact]
    public void AStartedAchievementShowsTheNextLevelFromWhereItBegan()
    {
        var level = AchievementLevelProgress.Resolve(Levels(1, 5, 25), levelsFinished: 2, levelCount: 3);

        Assert.Equal(3, level.Level);
        Assert.Equal(5, level.StartOfLevel);
        Assert.Equal(25, level.ScoreLimit);
        Assert.False(level.IsFinal);
    }

    [Fact]
    public void AFinishedAchievementStaysOnItsLastLevelRatherThanRunningOffTheEnd()
    {
        var level = AchievementLevelProgress.Resolve(Levels(1, 5, 25), levelsFinished: 3, levelCount: 3);

        Assert.Equal(3, level.Level);
        Assert.Equal(5, level.StartOfLevel);
        Assert.Equal(25, level.ScoreLimit);
        Assert.True(level.IsFinal);
    }

    [Fact]
    public void AFinishedSingleLevelAchievementStillHasABarToDraw()
    {
        // The bar has to run from zero: taking the level before it would make start equal the
        // limit and collapse it to nothing.
        var level = AchievementLevelProgress.Resolve(Levels(1), levelsFinished: 1, levelCount: 1);

        Assert.Equal(1, level.Level);
        Assert.Equal(0, level.StartOfLevel);
        Assert.Equal(1, level.ScoreLimit);
        Assert.True(level.IsFinal);
    }

    [Fact]
    public void TheBadgeComesFromTheLevelRatherThanBeingBuiltFromTheName()
    {
        // Not every badge is the name with the level on the end — ACH_VipParties1_Entry is a
        // first level whose badge carries no level — so it is taken from the level itself.
        var level = AchievementLevelProgress.Resolve(Levels(1, 5, 25), levelsFinished: 1, levelCount: 3);

        Assert.Equal("ACH_Test2", level.BadgeCode);
    }

    [Fact]
    public void ASparseDefinitionStillReportsTheLevelThePlayerIsOn()
    {
        // A definition read from a capture may hold one level out of twenty. A player who has
        // finished none is on level one, whatever the single row on record happens to be.
        var third = new AchievementLevel
        {
            Level = 3, BadgeCode = "ACH_RoomEntry3", RequiredProgress = 45, RewardPoints = 20,
        };

        var level = AchievementLevelProgress.Resolve([third], levelsFinished: 0, levelCount: 20);

        Assert.Equal(1, level.Level);
        Assert.False(level.IsFinal);

        // The badge belongs to level three, so it is not claimed for level one.
        Assert.Equal(string.Empty, level.BadgeCode);
    }

    [Fact]
    public void ASparseDefinitionUsesTheRowWhenThePlayerReachesIt()
    {
        var third = new AchievementLevel
        {
            Level = 3, BadgeCode = "ACH_RoomEntry3", RequiredProgress = 45, RewardPoints = 20,
        };

        var level = AchievementLevelProgress.Resolve([third], levelsFinished: 2, levelCount: 20);

        Assert.Equal(3, level.Level);
        Assert.Equal("ACH_RoomEntry3", level.BadgeCode);
        Assert.Equal(45, level.ScoreLimit);
    }

    [Fact]
    public void AStoredLevelBeyondTheLevelsIsTreatedAsFinished()
    {
        var level = AchievementLevelProgress.Resolve(Levels(1, 5), levelsFinished: 99, levelCount: 2);

        Assert.Equal(2, level.Level);
        Assert.True(level.IsFinal);
    }
}
