using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Notes on the walls: stuck up from a pad, read, rewritten and torn off. Anyone may stick
/// one up; without rights, they write it first and cannot change it after.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What the client puts between a note's colour and its text.</summary>
    private const char PostItSeparator = ' ';

    /// <summary>A pad whose count was never set holds one note: itself.</summary>
    private const int SingleNote = 1;

    public async Task PlacePostItAsync(Guid playerId, int padId, string location)
    {
        if (_entities?.Find(playerId) is null)
            return;

        // without rights the note is written before it goes up
        if (!IsController(playerId))
        {
            SendTo(playerId, new RequestSpamWallPostItMessageComposer(padId, location));
            return;
        }

        await StickPostItAsync(playerId, padId, location, furnitureOptions.Value.PostItDefaultColor, string.Empty);
    }

    public async Task AddSpamWallPostItAsync(Guid playerId, int padId, string location, string colorHex, string text)
    {
        if (_entities?.Find(playerId) is null || !IsPostItColor(colorHex))
            return;

        await StickPostItAsync(playerId, padId, location, colorHex, await PostItTextAsync(text));
    }

    /// <summary>Takes a note off the pad and sticks it up: the last note is the pad itself.</summary>
    private async Task StickPostItAsync(Guid playerId, int padId, string location, string colorHex, string text)
    {
        if (_room is null || _items is null || !WallLocation.TryParse(location, _model!, out var wall))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var pad = await LoadUnplacedItemAsync(db, padId, playerId, ItemType.Wall);
        if (pad?.Definition is null || interactions.For(pad.Definition) is not StickieInteraction)
            return;
        if (_items.Items.Count(i => i.Item.Definition!.Type == ItemType.Wall) >= _room.WallItemLimit)
        {
            SendTo(playerId, FurniLimitReached());
            return;
        }

        var padData = await furniData.LoadAsync(db, pad);
        var left = int.TryParse(padData.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : SingleNote;

        var note = pad;
        if (left > SingleNote)
        {
            note = new UserItem { OwnerId = playerId, DefinitionId = pad.DefinitionId, Definition = pad.Definition, Owner = pad.Owner };
            db.UserItems.Add(note);
            await FurniDataStore.SaveAsync(db, pad.Id, padData, padData with { State = (left - 1).ToString(CultureInfo.InvariantCulture) });
        }

        note.RoomId = _room.Id;
        note.WallPosition = wall.ToString();
        await db.SaveChangesAsync();

        var noteData = FurniDataView.WithState(colorHex);
        await FurniDataStore.SaveAsync(db, note.Id, left > SingleNote ? FurniDataView.Plain : padData, noteData);
        db.FurniPostIts.Add(new FurniPostIt { ItemId = note.Id, Text = text });
        await db.SaveChangesAsync();

        var placed = _items.AddWall(note, noteData);
        Broadcast(new ItemAddMessageComposer(WallItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: true)));
        if (left > SingleNote)
            SendTo(playerId, new PostItPlacedComposer(pad.Id, left - 1));
        else
            SendTo(playerId, FurniListRemoveComposer.Item(pad.Id));

        // a note in someone else's room: one left, and one they received
        if (playerId != _room.OwnerId)
        {
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(AchievementNames.NotesLeft, 1).Ignore();
            GrainFactory.GetGrain<IPlayerGrain>(_room.OwnerId).ProgressAchievementAsync(AchievementNames.NotesReceived, 1).Ignore();
        }
    }

    public async Task SendItemDataAsync(Guid playerId, int itemId)
    {
        if (_entities?.Find(playerId) is null || _items?.Find(itemId) is not { Interaction: StickieInteraction } note)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var text = await db.FurniPostIts.Where(p => p.ItemId == itemId).Select(p => p.Text).FirstOrDefaultAsync() ?? string.Empty;
        SendTo(playerId, new ItemDataUpdateMessageComposer(itemId, note.Data.StateText + PostItSeparator + text));
    }

    public async Task SetItemDataAsync(Guid playerId, int itemId, string colorHex, string text)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: StickieInteraction } note || !IsPostItColor(colorHex))
            return;

        text = await PostItTextAsync(text);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (await db.FurniPostIts.Where(p => p.ItemId == itemId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Text, text)) == 0)
            {
                db.FurniPostIts.Add(new FurniPostIt { ItemId = itemId, Text = text });
                await db.SaveChangesAsync();
            }
        }

        await _furniture!.UpdateAsync(note, note.Data with { State = colorHex });
        Broadcast(new ItemDataUpdateMessageComposer(itemId, colorHex + PostItSeparator + text));
    }

    /// <summary>Tears a note off the wall: it is gone, not back in anyone's hand.</summary>
    public async Task RemovePostItAsync(Guid playerId, int itemId)
    {
        if (IsController(playerId) && _items?.Find(itemId) is { Interaction: StickieInteraction } note)
            await _furniture!.RemoveAsync(note);
    }

    private bool IsPostItColor(string colorHex) =>
        furnitureOptions.Value.PostItColors.Contains(colorHex, StringComparer.OrdinalIgnoreCase);

    /// <summary>A note's text as it may stand: within the length, and through the room's filter.</summary>
    private async Task<string> PostItTextAsync(string text)
    {
        var limit = furnitureOptions.Value.PostItTextMaxLength;
        var trimmed = text.Length > limit ? text[..limit] : text;
        return (await wordFilter.FilterAsync(trimmed, _filterWords)).Text;
    }
}
