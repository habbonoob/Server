using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;

namespace Habbo.Silo.Tests;

/// <summary>Wired picking Builders Club furni: the dialog and the boxes go by the room's ids for it, what is saved by the
/// database's, which last when the room's do not.</summary>
public partial class RoomWiredTests
{
    /// <summary>Borrowed Builders Club furni on (x, y), with the database id given.</summary>
    private static RoomItemStore.PlacedItem BuildersClubFurni(Room room, int x, int y, int databaseId)
    {
        var definition = new ItemDefinition
        {
            Id = "bc_lamp".GetHashCode(),
            ClassName = "bc_lamp",
            Logic = "furniture_multistate",
            CanWalk = true,
            CanStackOn = true,
            StackHeight = 0.5,
            States = [new ItemDefinitionState { State = 0 }, new ItemDefinitionState { State = 1 }],
        };
        return room.Items.Add(new UserItem { Id = databaseId, Definition = definition, DefinitionId = definition.Id, BuildersClub = true },
            FurniDataView.WithState("0"), x, y, rotation: 0);
    }

    [Fact]
    public async Task APickedBuildersClubFurniIsSavedByItsDatabaseIdAndShownByItsRoomId()
    {
        var room = new Room();
        var lamp = BuildersClubFurni(room, 6, 1, databaseId: 500);
        var box = room.Box(1, 1, "wf_act_toggle_state", Effect, new FurniWired());
        FurniWired? stored = null;
        room.Wired.StoreSettings = settings =>
        {
            stored = settings;
            return Task.CompletedTask;
        };

        // the dialog picks it by the id the client has for it
        Assert.Equal(RoomItemStore.FirstBuildersClubId, lamp.Id);
        var answer = await room.Wired.SaveAsync(
            new WiredSaveData(WiredKind.Effect, box.Id, [], string.Empty, [lamp.Id], [], [], [], []), _ => true);

        Assert.IsType<WiredSaveSuccessComposer>(answer);
        Assert.Equal([500], stored!.StuffIds);
        Assert.Equal(box.Item.Id, stored.ItemId);
        var dialog = Assert.IsType<WiredFurniActionComposer>(room.Wired.DialogFor(box.Id));
        Assert.Equal([lamp.Id], dialog.Box.StuffIds);
    }

    [Fact]
    public async Task APickedBuildersClubFurniFollowsItsItemToTheIdTheRoomGivesItNextTime()
    {
        var room = new Room();
        var lamp = BuildersClubFurni(room, 6, 1, databaseId: 500);
        var box = room.Box(1, 1, "wf_act_toggle_state", Effect, new FurniWired());
        FurniWired? stored = null;
        room.Wired.StoreSettings = settings =>
        {
            stored = settings;
            return Task.CompletedTask;
        };
        await room.Wired.SaveAsync(new WiredSaveData(WiredKind.Effect, box.Id, [], string.Empty, [lamp.Id], [], [], [], []), _ => true);

        // loaded again with another borrowed item ahead of it: the lamp's room id is not what it was
        var again = new Room();
        BuildersClubFurni(again, 4, 3, databaseId: 400);
        var reloaded = BuildersClubFurni(again, 6, 1, databaseId: 500);
        var plate = again.Furni(3, 1, "plate");
        again.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var reloadedBox = again.Box(1, 1, "wf_act_toggle_state", Effect, again.Wired.InRoom(stored!));
        var user = again.User(2, 1);

        Assert.Equal(RoomItemStore.FirstBuildersClubId + 1, reloaded.Id);
        var dialog = Assert.IsType<WiredFurniActionComposer>(again.Wired.DialogFor(reloadedBox.Id));
        Assert.Equal([reloaded.Id], dialog.Box.StuffIds);

        await again.StepAsync(user, 3, 1);

        Assert.Equal("1", reloaded.Data.State);
    }

    [Fact]
    public async Task ASnapshotOfBuildersClubFurniIsSavedByItsDatabaseIdAndRestoredByItsRoomId()
    {
        var room = new Room();
        var lamp = BuildersClubFurni(room, 6, 1, databaseId: 500);
        var box = room.Box(1, 1, "wf_act_match_to_sshot", Effect, new FurniWired());
        FurniWired? stored = null;
        room.Wired.StoreSettings = settings =>
        {
            stored = settings;
            return Task.CompletedTask;
        };

        await room.Wired.SaveAsync(new WiredSaveData(WiredKind.Effect, box.Id, [1, 0, 0], string.Empty, [lamp.Id], [], [], [], []), _ => true);

        var snapshot = Assert.Single(stored!.Snapshots);
        Assert.Equal(500, snapshot.ItemId);
        Assert.Equal(box.Item.Id, snapshot.WiredItemId);

        var again = new Room();
        var reloaded = BuildersClubFurni(again, 6, 1, databaseId: 500);
        var settings = again.Wired.InRoom(stored);
        Assert.Equal(reloaded.Id, Assert.Single(settings.Snapshots).ItemId);
        Assert.Equal([reloaded.Id], settings.StuffIds);
    }

    [Fact]
    public void APickNoLongerInTheRoomKeepsItsDatabaseId()
    {
        var room = new Room();
        var settings = room.Wired.InRoom(new FurniWired { StuffIds = [500] });

        Assert.Equal([500], settings.StuffIds);
        Assert.Equal([500], room.Wired.Stored(settings).StuffIds);
    }

    [Fact]
    public void BuildersClubFurniIsSeenThroughWiredByItsRoomId()
    {
        var room = new Room();
        var lamp = BuildersClubFurni(room, 6, 1, databaseId: 500);

        Assert.Equal(lamp.Id, WiredHolderId(room, lamp));
    }

    /// <summary>What @id reads of the item.</summary>
    private static int? WiredHolderId(Room room, RoomItemStore.PlacedItem item) =>
        room.Wired.InternalVariables.Named(Habbo.Silo.Wired.Variables.WiredVariableTargets.Furni, "@id")!
            .Read(Habbo.Silo.Wired.Variables.WiredHolder.Of(item), null, room.Wired);
}
