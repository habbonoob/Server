using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>What the room keeps so wired's internal variables can read it at once: where each teleport leads (and where
/// wired sent it instead), how each user came in.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The teleport each of the room's teleports leads to, as linked, by database id as the links are saved;
    /// and where wired has pointed one instead (~teleport.target_id), by the ids the room knows them by, for as long
    /// as the room is loaded.</summary>
    private readonly Dictionary<int, int> _teleportLinks = [];
    private readonly Dictionary<int, int> _teleportTargets = [];

    /// <summary>How someone came in (@room_entry.method).</summary>
    private const int EnteredByDoor = 0;
    private const int EnteredByTeleport = 2;

    private async Task LoadTeleportLinksAsync(Habbo.Infrastructure.AppDbContext db, IReadOnlyCollection<int> teleportIds)
    {
        if (teleportIds.Count == 0)
            return;

        foreach (var link in await db.FurniTeleportLinks.AsNoTracking().Where(l => teleportIds.Contains(l.ItemId)).ToListAsync())
            _teleportLinks[link.ItemId] = link.LinkedItemId;
    }

    private Task LoadTeleportLinksAsync(Habbo.Infrastructure.AppDbContext db) =>
        LoadTeleportLinksAsync(db, _items!.Items.Where(i => i.Interaction is TeleportInteraction).Select(i => i.Item.Id).ToList());

    /// <summary>A teleport put down brings its link along.</summary>
    private async Task TeleportPlacedAsync(RoomItemStore.PlacedItem placed)
    {
        if (placed.Interaction is not TeleportInteraction)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await LoadTeleportLinksAsync(db, [placed.Item.Id]);
    }

    /// <summary>Where the teleport leads now, by the id the room knows it by: where wired pointed it, else its partner
    /// (by its database id when it stands in another room); null for one linked to nothing.</summary>
    private int? TeleportTargetOf(RoomItemStore.PlacedItem teleport) =>
        teleport.Interaction is not TeleportInteraction ? null
        : _teleportTargets.TryGetValue(teleport.Id, out var target) ? target
        : _teleportLinks.TryGetValue(teleport.Item.Id, out var linked) ? _items!.IdOfStored(linked)
        : null;

    /// <summary>Points the teleport at another teleport of this room, or back at its own partner, until the room unloads;
    /// its link in the database stays as it is.</summary>
    private Task<bool> SetTeleportTargetAsync(RoomItemStore.PlacedItem teleport, int targetId)
    {
        if (teleport.Interaction is not TeleportInteraction || targetId == teleport.Id)
            return Task.FromResult(false);

        if (_teleportLinks.TryGetValue(teleport.Item.Id, out var linked) && _items!.IdOfStored(linked) == targetId)
            _teleportTargets.Remove(teleport.Id);
        else if (_items?.Find(targetId) is { Interaction: TeleportInteraction })
            _teleportTargets[teleport.Id] = targetId;
        else
            return Task.FromResult(false);
        return Task.FromResult(true);
    }

    /// <summary>Where a teleport wired pointed elsewhere takes the avatar: the teleport in this room, by its database
    /// id as a link names it; null when it follows its link.</summary>
    private (int RoomId, int ItemId)? WiredTargetOf(RoomItemStore.PlacedItem teleport) =>
        _teleportTargets.TryGetValue(teleport.Id, out var target) && _items?.Find(target) is { } found ? (_room!.Id, found.Item.Id) : null;

    /// <summary>How the user came in: through the teleport they were expected at (by the id the room knows it by), or
    /// by the door.</summary>
    private static void RememberEntry(RoomEntityStore.RoomEntity entity, int? teleportId)
    {
        entity.EntryMethod = teleportId is null ? EnteredByDoor : EnteredByTeleport;
        entity.EntryTeleportId = teleportId;
    }

    /// <summary>A wall item moved by wired (@wallitem_offset): saved and shown as a move by hand is.</summary>
    private async Task<bool> MoveWallItemToAsync(RoomItemStore.PlacedItem placed, WallLocation location)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserItems.FirstOrDefaultAsync(i => i.Id == placed.Item.Id && i.RoomId == _room!.Id) is not { } row)
            return false;

        row.WallPosition = location.ToString();
        placed.Item.WallPosition = row.WallPosition;
        await db.SaveChangesAsync();
        Broadcast(new ItemUpdateMessageComposer(WallItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: false)));
        return true;
    }

    /// <summary>Shows a Habbicon over the avatar's head, for everyone, and remembers it for @habbicon.</summary>
    private void ShowHabbicon(RoomEntityStore.RoomEntity entity, int habbiconId)
    {
        entity.Habbicon = habbiconId;
        entity.HabbiconShownAt = DateTimeOffset.UtcNow;
        Broadcast(new RoomUseHabbiconComposer(entity.RoomIndex, habbiconId));
    }
}
