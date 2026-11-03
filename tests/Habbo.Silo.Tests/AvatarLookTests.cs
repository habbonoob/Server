using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

/// <summary>Where an avatar looks: a LookTo (a click on someone), as habbo.com answers one (October 2026).</summary>
public class AvatarLookTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private static (RoomItemStore Items, RoomEntityStore Entities) Room()
    {
        var model = RoomModelParser.Parse(string.Join("\r", Enumerable.Repeat(new string('0', 16), 16)), doorX: 0, doorY: 0);
        var items = new RoomItemStore(model, Interactions);
        return (items, new RoomEntityStore(model, items));
    }

    private static RoomEntityStore.RoomEntity Enter(RoomEntityStore entities, int x, int y, int direction)
    {
        var player = new PlayerData(
            Guid.NewGuid(), 1, "tester", "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true, false, false,
            ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");
        var user = entities.Enter(player.Id, Guid.NewGuid(), player, doorDirection: direction);
        entities.Warp(user, x, y);
        entities.Turn(user, direction);
        return user;
    }

    private static void Seat(RoomItemStore items, int x, int y, int rotation, bool bed = false)
    {
        var definition = new ItemDefinition
        {
            Id = bed ? 2 : 1, ClassName = bed ? "bed" : "chair", CanSit = !bed, CanLay = bed, CanWalk = bed, StackHeight = 0.5,
        };
        items.Add(new UserItem { Id = bed ? 21 : 20, Definition = definition, DefinitionId = definition.Id }, FurniDataView.Plain, x, y, rotation);
    }

    private static AvatarUpdateData? LookAndTick(RoomEntityStore entities, RoomEntityStore.RoomEntity user, int dx, int dy)
    {
        entities.LookToNextTick(user, user.X + dx, user.Y + dy);
        return entities.Tick().SingleOrDefault(update => update.RoomIndex == user.RoomIndex);
    }

    [Fact]
    public void ALookIsAnsweredOnTheNextTick()
    {
        // habbo.com: the UserUpdate comes 128-580 ms after the LookTo, on the room's half-second tick
        var (_, entities) = Room();
        var user = Enter(entities, 5, 5, direction: 0);

        entities.LookToNextTick(user, 5, 8);

        Assert.Equal(0, user.BodyDirection);
        var update = Assert.Single(entities.Tick());
        Assert.Equal((4, 4), (update.HeadDirection, update.BodyDirection));
    }

    [Theory]
    // within one eighth of the body: the head alone turns
    [InlineData(4, 3, 1, 3, 4)]
    [InlineData(6, -3, 5, 5, 6)]
    [InlineData(2, 5, -5, 1, 2)]
    [InlineData(2, 6, 1, 3, 2)]
    // further: body and head together, a diagonal body too
    [InlineData(0, 0, 2, 4, 4)]
    [InlineData(2, 1, 6, 4, 4)]
    [InlineData(0, -2, 3, 5, 5)]
    [InlineData(2, -5, -2, 7, 7)]
    // the direction is the angle rounded up to the next eighth, clockwise from north: not the nearest eighth, nor
    // the signs of the offset
    [InlineData(0, -3, 2, 6, 6)]
    [InlineData(0, 1, 3, 4, 4)]
    [InlineData(0, -6, 4, 6, 6)]
    public void AStandingAvatarTurnsItsHeadWithinAnEighthAndItsBodyBeyond(int body, int dx, int dy, int head, int newBody)
    {
        var (_, entities) = Room();
        var user = Enter(entities, 7, 7, body);

        var update = LookAndTick(entities, user, dx, dy);

        Assert.NotNull(update);
        Assert.Equal((head, newBody), (user.HeadDirection, user.BodyDirection));
    }

    [Theory]
    [InlineData(0, -1, 0, 0)]
    [InlineData(-1, 0, 6, 6)]
    [InlineData(1, -1, 1, 2)]
    [InlineData(1, 1, 3, 2)]
    // a diagonal past the head's reach: the head takes it, the body the straight direction clockwise of it
    [InlineData(-2, 2, 5, 6)]
    [InlineData(-2, -2, 7, 0)]
    public void AnAvatarSittingOnTheFloorLooksAsOneStandingButSitsStraight(int dx, int dy, int head, int body)
    {
        var (_, entities) = Room();
        var user = Enter(entities, 7, 7, direction: 2);
        entities.SitHere(user);

        var update = LookAndTick(entities, user, dx, dy);

        Assert.NotNull(update);
        Assert.Equal((head, body), (user.HeadDirection, user.BodyDirection));
        Assert.Contains("sit 0.55 1", update.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnAvatarOnAChairOrABedNeverTurnsButIsToldAgainWhenTheLookPointsElsewhere(bool bed)
    {
        var (items, entities) = Room();
        Seat(items, 7, 8, rotation: 2, bed);
        var user = Enter(entities, 7, 9, direction: 0);
        entities.Walk(user, 7, 8);
        while (user.Walking)
            entities.Tick();

        // habbo.com: north, behind and one eighth off all answer head 2 body 2; straight ahead answers nothing
        foreach (var (dx, dy) in new[] { (0, -1), (-1, 0), (1, -1) })
        {
            var update = LookAndTick(entities, user, dx, dy);
            Assert.NotNull(update);
            Assert.Equal((2, 2), (update.HeadDirection, update.BodyDirection));
        }

        Assert.Null(LookAndTick(entities, user, 2, -1));
    }

    [Fact]
    public void ALookThatChangesNothingIsNotTold()
    {
        var (_, entities) = Room();
        var user = Enter(entities, 7, 9, direction: 4);
        Assert.NotNull(LookAndTick(entities, user, 3, 1));

        Assert.Null(LookAndTick(entities, user, 2, 1));
        Assert.Equal((3, 4), (user.HeadDirection, user.BodyDirection));
    }
}
