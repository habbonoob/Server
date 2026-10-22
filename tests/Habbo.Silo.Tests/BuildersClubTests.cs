using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Tests;

public class BuildersClubTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Grace = TimeSpan.FromDays(7);

    private static BuildersClubView Status(DateTimeOffset? expiresAt) =>
        SubscriptionService.BuildersClubStatus(expiresAt, furniLimit: 50, Grace, maxFurniLimit: 500, Now);

    [Fact]
    public void OneWhoNeverJoinedHasNoLimitAndIsShownTheBestTier()
    {
        // Habbo, 2026-10-22: [0, 0, 500, 0]
        Assert.Equal(new BuildersClubView(0, 0, 500, 0), Status(null));
    }

    [Fact]
    public void AMemberHasTheTiersLimitAndTheGraceOnTop()
    {
        var status = Status(Now.AddDays(30));

        Assert.Equal(50, status.FurniLimit);
        Assert.Equal((int)TimeSpan.FromDays(30).TotalSeconds, status.SecondsLeft);
        Assert.Equal((int)TimeSpan.FromDays(37).TotalSeconds, status.SecondsLeftWithGrace);
        Assert.False(BuildersClubFurni.IsTrial(status));
    }

    [Fact]
    public void ThroughTheGracePeriodTheLimitHoldsButBuildingIsTheTrials()
    {
        var status = Status(Now.AddDays(-1));

        Assert.Equal(50, status.FurniLimit);
        Assert.Equal(0, status.SecondsLeft);
        Assert.True(BuildersClubFurni.IsTrial(status));
    }

    [Fact]
    public void AfterTheGracePeriodNothingMoreCanBeBorrowed()
    {
        var status = Status(Now.AddDays(-8));

        Assert.Equal(0, status.FurniLimit);
        Assert.False(BuildersClubFurni.HasRoomFor(0, status.FurniLimit));
    }

    [Fact]
    public void TheLimitIsHowManyMayBeOutAtOnce()
    {
        Assert.True(BuildersClubFurni.HasRoomFor(49, 50));
        Assert.False(BuildersClubFurni.HasRoomFor(50, 50));
    }

    [Fact]
    public void OnlyTheOwnerOrAGroupsAdminBuilds()
    {
        Assert.True(BuildersClubFurni.MayBuild(isOwner: true, RoomControllerLevels.None));
        Assert.True(BuildersClubFurni.MayBuild(isOwner: false, RoomControllerLevels.GroupAdmin));
        Assert.False(BuildersClubFurni.MayBuild(isOwner: false, RoomControllerLevels.Rights));
        Assert.False(BuildersClubFurni.MayBuild(isOwner: false, RoomControllerLevels.GroupRights));
    }

    [Fact]
    public void OnTheTrialNobodyMayBeVisitingWhileBuilding()
    {
        Assert.True(BuildersClubFurni.MayBuildWithVisitors(trial: false, visitors: 3));
        Assert.True(BuildersClubFurni.MayBuildWithVisitors(trial: true, visitors: 0));
        Assert.False(BuildersClubFurni.MayBuildWithVisitors(trial: true, visitors: 1));
    }

    [Fact]
    public void BorrowedFurniInNoRoomIsInNobodysInventory()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=none;Database=none").Options);

        var sql = db.UserItems.Where(item => item.RoomId == null).ToQueryString();

        Assert.Contains("NOT (u.\"BuildersClub\") OR u.\"RoomId\" IS NOT NULL", sql);
    }
}
