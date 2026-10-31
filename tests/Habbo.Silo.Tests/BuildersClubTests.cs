using Habbo.Domain.Subscriptions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
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
    public void OneWhoNeverJoinedIsOnTheTrialAndIsShownTheBestTier()
    {
        Assert.Equal(new BuildersClubView(0, BuildersClubFurni.TrialFurniLimit, 500, 0), Status(null));
        Assert.True(BuildersClubFurni.IsTrial(Status(null)));
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
    public void AfterTheGracePeriodItIsTheTrialAgain()
    {
        var status = Status(Now.AddDays(-8));

        Assert.Equal(BuildersClubFurni.TrialFurniLimit, status.FurniLimit);
        Assert.True(BuildersClubFurni.IsTrial(status));
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

    private static readonly BuildersClubLimits[] Tiers =
    [
        new() { ProductName = "builders_club", FurniLimit = 50, MaxFurniLimit = 500, Grace = Grace },
        new() { ProductName = "builders_club_pro", FurniLimit = 500, MaxFurniLimit = 500, Grace = Grace },
    ];

    private static Subscription Membership(string tier, DateTimeOffset expiresAt) =>
        new() { ProductName = tier, ExpiresAt = expiresAt };

    [Fact]
    public void BoughtDaysGoOnTopOfTheTimeLeft()
    {
        var membership = Membership("builders_club", Now.AddDays(5));

        var wasRunning = BuildersClubMembership.Extend(membership, "builders_club", TimeSpan.FromDays(31), 1, Tiers, Now);

        Assert.True(wasRunning);
        Assert.Equal(Now.AddDays(36), membership.ExpiresAt);
        Assert.Equal(Now, membership.LastModifiedAt);
    }

    [Fact]
    public void AMembershipWithNoTimeLeftRunsFromNowEvenInItsGrace()
    {
        var membership = Membership("builders_club", Now.AddDays(-2));

        var wasRunning = BuildersClubMembership.Extend(membership, "builders_club", TimeSpan.FromDays(31), 1, Tiers, Now);

        Assert.False(wasRunning);
        Assert.Equal(Now.AddDays(31), membership.ExpiresAt);
    }

    [Fact]
    public void BuyingNeverLowersTheTier()
    {
        var pro = Membership("builders_club_pro", Now.AddDays(5));
        BuildersClubMembership.Extend(pro, "builders_club", TimeSpan.FromDays(31), 1, Tiers, Now);
        Assert.Equal("builders_club_pro", pro.ProductName);

        var basic = Membership("builders_club", Now.AddDays(5));
        BuildersClubMembership.Extend(basic, "builders_club_pro", TimeSpan.FromDays(31), 1, Tiers, Now);
        Assert.Equal("builders_club_pro", basic.ProductName);
    }

    [Fact]
    public void AMembershipIsSentWithoutItsProductAsHabboSendsIt()
    {
        var offer = new ShopOffer(13902, "loyalty_bc_31_days", 105, ClubLevel.None, 1,
            [new ShopProduct(null, 0, false, "builders_club", 1, ProductTypes.BuildersClub, null, 31 * 86400)],
            105, 5, 0, false, true, false, string.Empty);

        Assert.Empty(CatalogOfferDataFactory.From(offer).Products);
    }
}
