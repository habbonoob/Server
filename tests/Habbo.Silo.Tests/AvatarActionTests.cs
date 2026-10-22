using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;

namespace Habbo.Silo.Tests;

public class AvatarActionTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private static (RoomItemStore Items, RoomEntityStore Entities, RoomEntityStore.RoomEntity User) Room()
    {
        var model = RoomModelParser.Parse("000000" + "\r" + "000000" + "\r" + "000000", doorX: 0, doorY: 0);
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var player = new PlayerData(
            Guid.NewGuid(), 1, "tester", "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true, false, false,
            ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");
        var user = entities.Enter(player.Id, Guid.NewGuid(), player, doorDirection: 2);
        return (items, entities, user);
    }

    private static void Seat(RoomItemStore items, int x, int y, bool bed = false, int rotation = 0)
    {
        var definition = new ItemDefinition
        {
            Id = bed ? 2 : 1, ClassName = bed ? "bed" : "chair", CanSit = !bed, CanLay = bed, CanWalk = bed, StackHeight = 0.5,
        };
        items.Add(new UserItem { Id = bed ? 21 : 20, Definition = definition, DefinitionId = definition.Id }, FurniDataView.Plain, x, y, rotation);
    }

    [Fact]
    public void ASignIsToldOnTheNextTickAndTakenDownOnTheOneAfter()
    {
        // Habbo: "/flatctrl 4/sit 0.55 1/sign 7/" on the tick after the click, the sign gone on the next
        var (_, entities, user) = Room();

        entities.ShowSign(user, 7);

        Assert.Contains(entities.Tick(), update => update.RoomIndex == user.RoomIndex && update.Status == "//sign 7/");
        Assert.Contains(entities.Tick(), update => update.RoomIndex == user.RoomIndex && update.Status == "//");
        Assert.Null(user.Sign);
    }

    [Fact]
    public void SittingOnTheFloorFromADiagonalFacesTheStraightDirectionBeforeIt()
    {
        var (_, entities, user) = Room();
        entities.Turn(user, 3);

        var update = entities.SitHere(user);

        Assert.NotNull(update);
        Assert.Equal(2, user.BodyDirection);
        Assert.Equal(2, user.HeadDirection);
    }

    [Fact]
    public void LyingOnTheFloorFromADiagonalFacesTheStraightDirectionBeforeIt()
    {
        var (_, entities, user) = Room();
        entities.Turn(user, 7);

        entities.LayHere(user);

        Assert.Equal(6, user.BodyDirection);
    }

    [Fact]
    public void SittingOnAChairFacesTheWayTheChairFaces()
    {
        var (items, entities, user) = Room();
        Seat(items, 2, 0, rotation: 4);

        entities.Walk(user, 2, 0);
        while (user.Walking)
            entities.Tick();

        Assert.True(user.Sitting);
        Assert.Equal(4, user.BodyDirection);
        Assert.Equal(4, user.HeadDirection);
    }

    [Fact]
    public void RotatingAChairTurnsWhoeverSitsOnIt()
    {
        var (items, entities, user) = Room();
        Seat(items, 2, 0, rotation: 4);
        entities.Walk(user, 2, 0);
        while (user.Walking)
            entities.Tick();

        items.MoveTo(items.Find(20)!, 2, 0, rotation: 6);
        var update = entities.Resettle(user);

        Assert.NotNull(update);
        Assert.True(user.Sitting);
        Assert.Equal(6, user.BodyDirection);
        Assert.Equal(6, update.BodyDirection);
    }

    [Fact]
    public void TakingAChairAwayStandsUpWhoeverSatOnIt()
    {
        var (items, entities, user) = Room();
        Seat(items, 2, 0);
        entities.Walk(user, 2, 0);
        while (user.Walking)
            entities.Tick();

        items.Remove(20);
        entities.Resettle(user);

        Assert.False(user.Sitting);
    }

    [Fact]
    public void SittingDownStopsADanceAndTellsTheRoom()
    {
        var (items, entities, user) = Room();
        Seat(items, 2, 0);
        entities.Dance(user, 1);

        entities.Walk(user, 2, 0);
        while (user.Walking)
            entities.Tick();

        Assert.True(user.Sitting);
        Assert.Equal(0, user.DanceStyle);
        var changes = entities.TakeChanges().Select(c => c.Change).ToList();
        Assert.Contains(RoomEntityStore.EntityChange.DanceStopped, changes);
        Assert.Contains(RoomEntityStore.EntityChange.SatDown, changes);
    }

    [Fact]
    public void ABedLaysTheAvatarDown()
    {
        var (items, entities, user) = Room();
        Seat(items, 3, 1, bed: true);

        var update = entities.Warp(user, 3, 1);

        Assert.True(user.Laying);
        Assert.StartsWith("/lay ", update.Status);
    }

    [Fact]
    public void AnIdleAvatarFallsAsleepAndWakesWhenItActs()
    {
        var (_, entities, user) = Room();
        entities.SleepAfterTicks = 600;

        for (var tick = 0; tick < 599; tick++)
            entities.Tick();
        Assert.False(user.Asleep);

        entities.Tick();
        Assert.True(user.Asleep);
        Assert.Contains(entities.TakeChanges(), c => c.Change == RoomEntityStore.EntityChange.FellAsleep);
        Assert.True(entities.Wake(user));
        Assert.False(user.Asleep);
    }

    [Fact]
    public void AnAvatarIdleForAsLongAsTheRoomLetsIsSentOut()
    {
        var (_, entities, user) = Room();
        entities.SleepAfterTicks = 2;
        entities.SendOutAfterTicks = 4;

        for (var tick = 0; tick < 3; tick++)
            entities.Tick();
        Assert.DoesNotContain(entities.TakeChanges(), c => c.Change == RoomEntityStore.EntityChange.IdledOut);

        entities.Tick();
        Assert.Contains(entities.TakeChanges(), c => c.Entity == user && c.Change == RoomEntityStore.EntityChange.IdledOut);
    }

    [Fact]
    public void AnIdleTimeThatIsOffNeverComes()
    {
        var (_, entities, user) = Room();

        for (var tick = 0; tick < 2000; tick++)
            entities.Tick();

        Assert.False(user.Asleep);
        Assert.Empty(entities.TakeChanges().Where(c => c.Change is RoomEntityStore.EntityChange.FellAsleep or RoomEntityStore.EntityChange.IdledOut));
    }

    [Fact]
    public void AMomentsActionCountsForTwoSecondsAndAPostureForAsLongAsItIsHeld()
    {
        var (_, entities, user) = Room();
        entities.RecordAction(user, WiredUserActions.Wave, "");

        Assert.True(WiredUserActions.IsPerforming(user, WiredUserActions.Wave, "", entities.CurrentTick));
        for (var tick = 0; tick <= WiredUserActions.MomentTicks; tick++)
            entities.Tick();
        Assert.False(WiredUserActions.IsPerforming(user, WiredUserActions.Wave, "", entities.CurrentTick));

        entities.Dance(user, 3);
        Assert.True(WiredUserActions.IsPerforming(user, WiredUserActions.Dance, "dance 3", entities.CurrentTick));
        Assert.True(WiredUserActions.IsPerforming(user, WiredUserActions.Dance, "", entities.CurrentTick));
        Assert.False(WiredUserActions.IsPerforming(user, WiredUserActions.Dance, "dance 2", entities.CurrentTick));
        Assert.True(WiredUserActions.IsPerforming(user, WiredUserActions.Stand, "", entities.CurrentTick));
    }
}
