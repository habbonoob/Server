using System.Text.Json;
using Habbo.Infrastructure;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Habbo.Silo.Wired.Variables;
using Microsoft.Extensions.Configuration;

namespace Habbo.Silo.Tests;

/// <summary>habbo.com's internal and smart variables, read from the seed once for every test.</summary>
public static class InternalVariableSeed
{
    public static SeedFiles Files { get; } = new(new ConfigurationBuilder().Build());

    public static WiredInternalVariables Variables { get; } =
        new WiredInternalVariableCatalog(Files).LoadAsync().GetAwaiter().GetResult();
}

public class WiredInternalVariableCatalogTests
{
    private static JsonElement[] SeedVariables()
    {
        using var stream = InternalVariableSeed.Files.OpenRead(WiredInternalVariableCatalog.SeedFile)!;
        return JsonDocument.Parse(stream).RootElement.GetProperty("variables").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    [Fact]
    public void EveryOneOfHabbosVariablesIsListedAsHabboDefinesIt()
    {
        var seed = SeedVariables();
        var listed = InternalVariableSeed.Variables.All.Select(v => v.Definition.ToWire()).ToList();

        Assert.Equal(162, seed.Length);
        Assert.Equal(162, listed.Count);
        for (var i = 0; i < seed.Length; i++)
        {
            var (expected, wire) = (seed[i], listed[i]);
            Assert.Equal(expected.GetProperty("id").GetString(), wire.Id);
            Assert.Equal(expected.GetProperty("kind").GetInt32(), wire.VariableType);
            Assert.Equal(expected.GetProperty("name").GetString(), wire.Name);
            Assert.Equal(expected.GetProperty("availability").GetInt32(), wire.Availability);
            Assert.Equal(expected.GetProperty("target").GetInt32(), wire.Target);
            Assert.Equal(expected.GetProperty("alwaysAvailable").GetBoolean(), wire.AlwaysAvailable);
            Assert.Equal(expected.GetProperty("canCreateAndDelete").GetBoolean(), wire.CanCreateAndDelete);
            Assert.Equal(expected.GetProperty("hasValue").GetBoolean(), wire.HasValue);
            Assert.Equal(expected.GetProperty("canWriteValue").GetBoolean(), wire.CanWriteValue);
            Assert.Equal(expected.GetProperty("canInterceptChanges").GetBoolean(), wire.CanInterceptChanges);
            Assert.Equal(expected.GetProperty("isInvisible").GetBoolean(), wire.IsInvisible);
            Assert.Equal(expected.GetProperty("requiresExecutionContext").GetBoolean(), wire.RequiresExecutionContext);
            Assert.Equal(expected.GetProperty("canReadCreationTime").GetBoolean(), wire.CanReadCreationTime);
            Assert.Equal(expected.GetProperty("canReadLastUpdateTime").GetBoolean(), wire.CanReadLastUpdateTime);

            var hasTexts = expected.TryGetProperty("texts", out var texts);
            Assert.Equal(hasTexts, wire.HasTextConnector);
            if (hasTexts)
                Assert.Equal(texts.EnumerateObject().Select(t => (int.Parse(t.Name), t.Value.GetString()!)),
                    wire.TextConnector!.Select(t => (t.Value, t.Text)));
        }
    }

    [Fact]
    public void APackedVariableKeepsItsEmptyTextConnector()
    {
        var position = InternalVariableSeed.Variables.Find("-125")!.Definition.ToWire();

        Assert.True(position.HasTextConnector);
        Assert.Empty(position.TextConnector!);
        Assert.Equal(4, InternalVariableSeed.Variables.Find("-205")!.Definition.TextConnector.Count);
    }

    [Fact]
    public void EveryVariableTheServerReadsIsOneOfHabbos()
    {
        foreach (var (target, name) in WiredInternalVariableAccess.Names)
            Assert.True(InternalVariableSeed.Variables.Named(target, name) is not null, $"{target} {name}");
    }

    [Fact]
    public void OnlyWhatHabboLetsBeWrittenIsWritten() =>
        Assert.All(InternalVariableSeed.Variables.All.Where(v => v.IsWritten), v => Assert.True(v.Definition.CanWriteValue));

    [Fact]
    public void TheListIsTheSameEveryTimeItIsLoaded()
    {
        var again = new WiredInternalVariableCatalog(InternalVariableSeed.Files).LoadAsync().GetAwaiter().GetResult();

        Assert.Equal(InternalVariableSeed.Variables.All.Select(v => v.Definition.Hash), again.All.Select(v => v.Definition.Hash));
    }
}

public partial class RoomWiredTests
{
    private static WiredVariable Internal(Room room, string id) => room.Wired.FindVariable(id)!;

    [Fact]
    public void TheRoomListsEveryInternalVariableFirstAndHashesThemStably()
    {
        var room = new Room();

        Assert.Equal(InternalVariableSeed.Variables.All.Select(v => v.Definition.Id), room.Wired.Variables.Take(162).Select(v => v.Id));
        Assert.Equal(new Room().Wired.VariablesHash, room.Wired.VariablesHash);
    }

    [Fact]
    public async Task AFurnisPositionRotationAndAltitudeAreRead()
    {
        var room = new Room();
        var ball = room.Furni(3, 2, "ball");
        await room.Wired.SetAltitudeAsync(ball, 1.25);
        var holder = WiredHolder.Of(ball);

        Assert.Equal(3, room.Wired.VariableValue(Internal(room, "-120"), holder, null));
        Assert.Equal(2, room.Wired.VariableValue(Internal(room, "-121"), holder, null));
        Assert.Equal(0, room.Wired.VariableValue(Internal(room, "-122"), holder, null));
        Assert.Equal(125, room.Wired.VariableValue(Internal(room, "-123"), holder, null));
        Assert.Equal(3 << 8 | 2, room.Wired.VariableValue(Internal(room, "-125"), holder, null));
        Assert.Equal(50, room.Wired.VariableValue(Internal(room, "-102"), holder, null));
    }

    [Fact]
    public void AFurnisFlagsAreHeldOnlyWhenTheyHold()
    {
        var room = new Room();
        var holder = WiredHolder.Of(room.Furni(1, 1));

        // the test furni can be walked on and stacked on, not sat on
        Assert.True(room.Wired.HasVariable(Internal(room, "-131"), holder, null));
        Assert.Equal(0, room.Wired.VariableValue(Internal(room, "-131"), holder, null));
        Assert.False(room.Wired.HasVariable(Internal(room, "-132"), holder, null));
    }

    [Fact]
    public async Task AFurnisPositionIsWritten()
    {
        var room = new Room();
        var ball = room.Furni(1, 1, "ball");
        var holder = WiredHolder.Of(ball);

        Assert.True(await room.Wired.SetVariableAsync(Internal(room, "-121"), holder, null, 3));
        Assert.Equal((1, 3), (ball.X, ball.Y));
        Assert.True(await room.Wired.SetVariableAsync(Internal(room, "-125"), holder, null, 5 << 8 | 2));
        Assert.Equal((5, 2), (ball.X, ball.Y));
    }

    [Fact]
    public async Task AUsersDirectionIsReadAndWritten()
    {
        var room = new Room();
        var user = room.User(2, 2);
        var direction = Internal(room, "-232");
        var holder = WiredHolder.Of(user);

        Assert.Equal(user.BodyDirection, room.Wired.VariableValue(direction, holder, null));
        Assert.True(await room.Wired.SetVariableAsync(direction, holder, null, 6));
        Assert.Equal(6, room.Wired.VariableValue(direction, holder, null));
        Assert.Equal(2, room.Wired.VariableValue(Internal(room, "-230"), holder, null));
        Assert.Equal(1, room.Wired.VariableValue(Internal(room, "-205"), holder, null));
    }

    [Fact]
    public async Task AUsersHandItemIsGivenAndTaken()
    {
        var room = new Room();
        var user = room.User(2, 2);
        var handItem = Internal(room, "-240");
        var holder = WiredHolder.Of(user);

        Assert.False(room.Wired.HasVariable(handItem, holder, null));
        Assert.True(await room.Wired.GiveVariableAsync(handItem, holder, null, 7, overrideExisting: false));
        Assert.Equal(7, room.Wired.VariableValue(handItem, holder, null));
        Assert.True(await room.Wired.RemoveVariableAsync(handItem, holder, null));
        Assert.Equal(0, user.HandItem);
    }

    [Fact]
    public void TheRoomCountsItsFurniAndUsers()
    {
        var room = new Room();
        room.Furni(1, 1);
        room.Furni(2, 1);
        room.User(3, 3);

        Assert.Equal(2, room.Wired.VariableValue(Internal(room, "-300"), WiredHolder.Room, null));
        Assert.Equal(1, room.Wired.VariableValue(Internal(room, "-301"), WiredHolder.Room, null));
    }

    [Fact]
    public async Task ATeamsScoreIsWritten()
    {
        var room = new Room();
        var red = Internal(room, "-320");

        Assert.True(await room.Wired.SetVariableAsync(red, WiredHolder.Room, null, 12));
        Assert.Equal(12, room.Games.Score(RoomGames.Red));
        Assert.Equal(12, room.Wired.VariableValue(red, WiredHolder.Room, null));
    }

    [Fact]
    public void TheTimeIsReadInItsParts()
    {
        var room = new Room();
        // a Sunday
        var now = new DateTimeOffset(2026, 10, 25, 14, 7, 9, 345, TimeSpan.Zero);
        room.Wired.Clock = () => now;
        int? Part(string id) => room.Wired.VariableValue(Internal(room, id), WiredHolder.Room, null);

        Assert.Equal((int)now.ToUnixTimeSeconds(), Part("-1000"));
        Assert.Equal(345, Part("-1001"));
        Assert.Equal(9, Part("-1002"));
        Assert.Equal(7, Part("-1003"));
        Assert.Equal(14, Part("-1004"));
        Assert.Equal(7, Part("-1005"));
        Assert.Equal(25, Part("-1006"));
        Assert.Equal(298, Part("-1007"));
        Assert.Equal(43, Part("-1008"));
        Assert.Equal(10, Part("-1009"));
        Assert.Equal(2026, Part("-1010"));
    }

    [Fact]
    public void AFurniThatNeverFlewHoldsNoProjectileVariable()
    {
        var room = new Room();
        var ball = room.Furni(1, 1, "ball");
        var projectile = Internal(room, "-2103");

        Assert.Contains(room.Wired.Variables, v => v.Id == "-2103");
        Assert.False(room.Wired.HasVariable(projectile, WiredHolder.Of(ball), null));
        Assert.Null(room.Wired.VariableValue(projectile, WiredHolder.Of(ball), null));
    }

    [Fact]
    public async Task ARecolourableFurnisColourIsReadAndWritten()
    {
        var room = new Room();
        var lamp = room.Furni(1, 1, "recolour_lamp", WiredInternalVariableAccess.RecolorableLogic);
        var holder = WiredHolder.Of(lamp);
        var rgb = Internal(room, "~146");

        // a furni never recoloured is white, as the client draws it
        Assert.Equal(0xFFFFFF, room.Wired.VariableValue(rgb, holder, null));
        Assert.True(await room.Wired.SetVariableAsync(rgb, holder, null, 0x112233));
        Assert.Equal(0x112233, room.Wired.VariableValue(rgb, holder, null));
        Assert.Equal(0x11, room.Wired.VariableValue(Internal(room, "~140"), holder, null));
        Assert.Equal(0x33, room.Wired.VariableValue(Internal(room, "~142"), holder, null));
        Assert.Contains(lamp.Data.MapEntries!, e => e is { Key: "state", Value: "0" });
        Assert.Null(room.Wired.VariableValue(rgb, WiredHolder.Of(room.Furni(2, 2)), null));
    }

    [Fact]
    public void AWallItemsOffsetIsRead()
    {
        var room = new Room();
        var poster = room.Furni(0, 0, "poster");
        poster.Item.WallPosition = ":w=1,2 l=-5,30 r";

        Assert.Equal(-5 << 16 | 30, room.Wired.VariableValue(Internal(room, "-190"), WiredHolder.Of(poster), null));
        Assert.Null(room.Wired.VariableValue(Internal(room, "-190"), WiredHolder.Of(room.Furni(1, 1)), null));
    }

    [Fact]
    public async Task AFurniGivenToMoveFreelyMovesOntoAnAvatar()
    {
        var room = new Room();
        var ball = room.Furni(1, 1, "ball");
        room.User(2, 1);
        var holder = WiredHolder.Of(ball);

        Assert.False(await room.Wired.MoveItemAsync(ball, 2, 1, 0, WiredMoveOptions.Default));
        Assert.True(await room.Wired.GiveVariableAsync(Internal(room, "-2000"), holder, null, 0, overrideExisting: false));
        Assert.True(room.Wired.HasVariable(Internal(room, "-2000"), holder, null));
        Assert.True(await room.Wired.MoveItemAsync(ball, 2, 1, 0, WiredMoveOptions.Default));
        Assert.True(await room.Wired.RemoveVariableAsync(Internal(room, "-2000"), holder, null));
        Assert.False(ball.MovesFreely);
    }

    [Fact]
    public void AProjectilesFlightIsReadAsTheClientDrawsIt()
    {
        var room = new Room();
        var ball = room.Furni(1, 1, "ball");
        var start = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        ball.Projectile = new ProjectileFlight(1, 1, 0, 5, 1, 0, start, 1000) { UserCollisions = 1 };
        var holder = WiredHolder.Of(ball);
        int? Read(string id) => room.Wired.VariableValue(Internal(room, id), holder, null);

        room.Wired.Clock = () => start.AddMilliseconds(500);
        Assert.Equal(4, Read("-2100"));
        Assert.Equal(1, Read("-2101"));
        Assert.Equal(0, Read("-2102"));
        Assert.Equal(3, Read("-2103"));
        Assert.Equal(1, Read("-2104"));
        Assert.Equal(0, Read("-2106"));

        room.Wired.Clock = () => start.AddSeconds(2);
        Assert.Equal(5, Read("-2103"));
        Assert.Null(Read("-2106"));
    }

    [Fact]
    public async Task AUsersTeamIsInTheGameTheyJoined()
    {
        var room = new Room();
        var user = room.User(1, 1);
        var holder = WiredHolder.Of(user);

        Assert.Null(room.Wired.VariableValue(Internal(room, "-239"), holder, null));
        await room.Games.JoinAsync(user, RoomGames.Red, effectId: 0);
        Assert.Equal(RoomGames.WiredGame, room.Wired.VariableValue(Internal(room, "-239"), holder, null));
        Assert.Equal(RoomGames.Red, room.Wired.VariableValue(Internal(room, "-238"), holder, null));
        await room.Games.LeaveAsync(user);
        Assert.Null(room.Wired.VariableValue(Internal(room, "-239"), holder, null));
    }

    [Fact]
    public async Task AUsersHabbiconIsShownAndPutAway()
    {
        var room = new Room();
        var user = room.User(1, 1);
        var habbicon = Internal(room, "-245");
        var holder = WiredHolder.Of(user);

        Assert.False(room.Wired.HasVariable(habbicon, holder, null));
        Assert.True(await room.Wired.GiveVariableAsync(habbicon, holder, null, 28, overrideExisting: false));
        Assert.Equal(28, room.Wired.VariableValue(habbicon, holder, null));
        Assert.NotNull(room.Wired.VariableTimes(habbicon, holder, null));
        Assert.True(await room.Wired.RemoveVariableAsync(habbicon, holder, null));
        Assert.Null(user.Habbicon);
    }

    [Fact]
    public void AUserCameInByTheDoorUnlessATeleportBroughtThem()
    {
        var room = new Room();
        var user = room.User(1, 1);
        var holder = WiredHolder.Of(user);

        Assert.Equal(0, room.Wired.VariableValue(Internal(room, "-266"), holder, null));
        Assert.Null(room.Wired.VariableValue(Internal(room, "-267"), holder, null));
        user.EntryMethod = 2;
        user.EntryTeleportId = 9;
        Assert.Equal(9, room.Wired.VariableValue(Internal(room, "-267"), holder, null));
    }

    [Fact]
    public void EveryInternalVariableIsRead() =>
        Assert.Empty(InternalVariableSeed.Variables.All.Where(v => !v.IsRead).Select(v => v.Definition.Id));
}
