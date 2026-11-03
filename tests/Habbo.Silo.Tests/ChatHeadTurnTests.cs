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

/// <summary>A listener's head turning toward chat, as habbo.com turns it (October 2026).</summary>
public class ChatHeadTurnTests
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

    private static AvatarUpdateData? UpdateOf(List<AvatarUpdateData> updates, RoomEntityStore.RoomEntity user) =>
        updates.SingleOrDefault(update => update.RoomIndex == user.RoomIndex);

    private static AvatarUpdateData? HearAndTick(
        RoomEntityStore entities, RoomEntityStore.RoomEntity listener, RoomEntityStore.RoomEntity speaker)
    {
        entities.Heard(listener, speaker);
        return UpdateOf(entities.Tick(), listener);
    }

    [Fact]
    public void AListenersHeadTurnsOneEighthTowardTheSpeakerOnTheNextTick()
    {
        var (_, entities) = Room();
        var listener = Enter(entities, 7, 7, direction: 2);
        var speaker = Enter(entities, 8, 5, direction: 4);

        entities.Heard(listener, speaker);
        entities.Heard(speaker, speaker);

        Assert.Equal(2, listener.HeadDirection);
        var updates = entities.Tick();
        var update = UpdateOf(updates, listener);
        Assert.NotNull(update);
        Assert.Equal((1, 2), (update.HeadDirection, update.BodyDirection));
        Assert.Null(UpdateOf(updates, speaker));
    }

    [Theory]
    // degrees clockwise from the body (east) on habbo.com: +5.2, +9.5 and +90 turn clockwise; +104 and behind do not
    [InlineData(11, 1, 3)]
    [InlineData(6, 1, 3)]
    [InlineData(0, 1, 3)]
    [InlineData(-1, 4, null)]
    [InlineData(-1, 2, null)]
    // straight ahead, -9.5 and -33.7 do not; -45, -116.6 and further round to -135 (not included) turn anticlockwise
    [InlineData(4, 0, null)]
    [InlineData(6, -1, null)]
    [InlineData(3, -2, null)]
    [InlineData(1, -1, 1)]
    [InlineData(-1, -2, 1)]
    [InlineData(-1, -4, 1)]
    [InlineData(-1, -6, 1)]
    [InlineData(-1, -1, null)]
    public void TheHeadTurnsOnlyTowardLinesWithinTwoEighthsOfTheBody(int dx, int dy, int? head)
    {
        var (_, entities) = Room();
        var listener = Enter(entities, 2, 7, direction: 2);
        var speaker = Enter(entities, 2 + dx, 7 + dy, direction: 4);

        var update = HearAndTick(entities, listener, speaker);

        Assert.Equal(head, update?.HeadDirection);
        Assert.Equal(2, listener.BodyDirection);
    }

    [Fact]
    public void TheHeadGoesBackTenTicksLaterAndALineMeanwhileStartsThatOver()
    {
        var (_, entities) = Room();
        var listener = Enter(entities, 7, 7, direction: 2);
        var speaker = Enter(entities, 8, 5, direction: 4);
        Assert.NotNull(HearAndTick(entities, listener, speaker));

        for (var tick = 1; tick < 5; tick++)
            Assert.Null(UpdateOf(entities.Tick(), listener));

        // a second line with the head already turned: nothing new, but the ten ticks start again
        Assert.Null(HearAndTick(entities, listener, speaker));
        for (var tick = 1; tick < RoomEntityStore.HeadBackTicks; tick++)
            Assert.Null(UpdateOf(entities.Tick(), listener));

        var back = UpdateOf(entities.Tick(), listener);
        Assert.NotNull(back);
        Assert.Equal((2, 2), (back.HeadDirection, back.BodyDirection));
    }

    [Fact]
    public void SleepingAndWalkingListenersDoNotTurn()
    {
        var (_, entities) = Room();
        var sleeper = Enter(entities, 7, 7, direction: 2);
        var walker = Enter(entities, 3, 10, direction: 2);
        var speaker = Enter(entities, 8, 5, direction: 4);
        entities.Sleep(sleeper);
        entities.Walk(walker, 6, 10);
        entities.Tick();

        entities.Heard(sleeper, speaker);
        entities.Heard(walker, speaker);
        var updates = entities.Tick();

        Assert.Null(UpdateOf(updates, sleeper));
        Assert.Equal(2, sleeper.HeadDirection);
        Assert.Equal(walker.BodyDirection, UpdateOf(updates, walker)?.HeadDirection);
    }

    [Fact]
    public void AListenerOnAChairIsToldAgainUnchangedAndNothingTurnsBack()
    {
        var (items, entities) = Room();
        var chair = new ItemDefinition { Id = 1, ClassName = "chair", CanSit = true, StackHeight = 0.5 };
        items.Add(new UserItem { Id = 20, Definition = chair, DefinitionId = chair.Id }, FurniDataView.Plain, 5, 6, 2);
        var listener = Enter(entities, 5, 7, direction: 0);
        entities.Walk(listener, 5, 6);
        while (listener.Walking)
            entities.Tick();

        // habbo.com, chair facing east: +26.6 and -63.4 degrees each give one unchanged update, -26.6 none
        foreach (var (x, y, told) in new[] { (7, 7, true), (6, 4, true), (7, 5, false) })
        {
            var speaker = Enter(entities, x, y, direction: 4);
            var update = HearAndTick(entities, listener, speaker);
            Assert.Equal(told, update is not null);
            if (update is not null)
                Assert.Equal((2, 2, true), (update.HeadDirection, update.BodyDirection, update.Status.Contains("sit ")));
            entities.Leave(speaker.Player.Id);
        }

        for (var tick = 0; tick < RoomEntityStore.HeadBackTicks + 2; tick++)
            Assert.Null(UpdateOf(entities.Tick(), listener));
    }

    [Fact]
    public void AListenerSittingOnTheFloorTurnsAndKeepsSitting()
    {
        var (_, entities) = Room();
        var listener = Enter(entities, 7, 7, direction: 2);
        var speaker = Enter(entities, 8, 5, direction: 4);
        entities.SitHere(listener);

        var update = HearAndTick(entities, listener, speaker);

        Assert.NotNull(update);
        Assert.Equal((1, 2), (update.HeadDirection, update.BodyDirection));
        Assert.Contains("sit 0.55 1", update.Status);
    }

    [Fact]
    public void ALineFromStraightAheadPutsALookingHeadBack()
    {
        var (_, entities) = Room();
        var listener = Enter(entities, 5, 6, direction: 2);
        entities.LookToNextTick(listener, 6, 7);
        entities.Tick();
        Assert.Equal(3, listener.HeadDirection);

        var speaker = Enter(entities, 7, 6, direction: 6);
        var update = HearAndTick(entities, listener, speaker);

        Assert.NotNull(update);
        Assert.Equal((2, 2), (update.HeadDirection, update.BodyDirection));
    }

    [Fact]
    public void ALookAfterALineIsTurnedBackWhenTheLinesTimeIsUp()
    {
        // habbo.com: the look does not start the ten ticks over, and the head goes back to the body at the line's time
        var (_, entities) = Room();
        var listener = Enter(entities, 7, 7, direction: 2);
        var speaker = Enter(entities, 8, 5, direction: 4);
        Assert.NotNull(HearAndTick(entities, listener, speaker));
        entities.Tick();
        entities.Tick();

        entities.LookToNextTick(listener, 8, 8);
        Assert.Equal(3, UpdateOf(entities.Tick(), listener)?.HeadDirection);
        for (var tick = 4; tick < RoomEntityStore.HeadBackTicks; tick++)
            Assert.Null(UpdateOf(entities.Tick(), listener));

        Assert.Equal(2, UpdateOf(entities.Tick(), listener)?.HeadDirection);
    }
}
