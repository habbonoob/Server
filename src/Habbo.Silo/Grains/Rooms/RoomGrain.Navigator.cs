using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>What the navigator shows of the room from inside it: its likes, its running promotion,
/// its staff pick; and where its visitors came from.</summary>
public sealed partial class RoomGrain
{
    /// <summary>A like: the only rating the client offers.</summary>
    private const int LikePoints = 1;

    /// <summary>The room's promotion, while one runs.</summary>
    private RoomEventInfo? _event;

    private RoomEventInfo? ActiveEvent => _event is { } running && running.ExpiresAt > DateTimeOffset.UtcNow ? running : null;

    private async Task LoadEventAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        _event = await db.RoomEvents.AsNoTracking()
            .Where(e => e.RoomId == (int)this.GetPrimaryKeyLong() && e.ExpiresAt > now)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new RoomEventInfo(e.Id, e.Owner!.AccountId, e.Owner.Username, e.RoomId, e.Type,
                e.Name, e.Description, e.CreatedAt, e.ExpiresAt, e.CategoryId))
            .FirstOrDefaultAsync();
    }

    /// <summary>Whether this visitor may still like the room: not their own, and not liked before.</summary>
    private async Task<bool> CanRateAsync(Guid playerId)
    {
        if (_room is null || _room.OwnerId == playerId)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        return !await db.RoomVotes.AnyAsync(v => v.RoomId == _room.Id && v.UserId == playerId);
    }

    public async Task RateAsync(Guid playerId, int points)
    {
        if (points != LikePoints || _entities?.Find(playerId) is null || !await CanRateAsync(playerId))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomVotes.Add(new RoomVote { RoomId = _room!.Id, UserId = playerId });
        await db.SaveChangesAsync();
        await db.Rooms.Where(r => r.Id == _room.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Score, r => r.Score + points));
        _room.Score += points;

        SendTo(playerId, new RoomRatingComposer(_room.Score, CanRate: false));
    }

    /// <summary>The room's owner and staff run its promotion.</summary>
    private bool MayRunEvent(PlayerData player) =>
        _room is not null && (_room.OwnerId == player.Id || permissions.Has(player, PermissionNames.RoomManageAny));

    public async Task EditEventAsync(PlayerData player, int eventId, string name, string description)
    {
        if (ActiveEvent is not { } running || running.Id != eventId || !MayRunEvent(player)
            || string.IsNullOrWhiteSpace(name) || name.Length > RoomEvent.NameMaxLength || description.Length > RoomEvent.DescriptionMaxLength)
            return;

        // a name or description with words the hotel filters is not saved: the editor shows it filtered, to put right
        var (filteredName, nameHit) = await wordFilter.FilterAsync(name, _filterWords);
        if (nameHit is not null)
        {
            SendTo(player.Id, new Habbo.Protocol.Messages.Outgoing.Advertisement.RoomAdErrorComposer(Habbo.Protocol.Messages.Outgoing.Advertisement.RoomAdErrorComposer.FilteredName, filteredName));
            return;
        }

        var (filteredDescription, descriptionHit) = await wordFilter.FilterAsync(description, _filterWords);
        if (descriptionHit is not null)
        {
            SendTo(player.Id, new Habbo.Protocol.Messages.Outgoing.Advertisement.RoomAdErrorComposer(Habbo.Protocol.Messages.Outgoing.Advertisement.RoomAdErrorComposer.FilteredDescription, filteredDescription));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.RoomEvents.Where(e => e.Id == eventId).ExecuteUpdateAsync(s => s
            .SetProperty(e => e.Name, name)
            .SetProperty(e => e.Description, description));

        _event = running with { Name = name, Description = description };
        Broadcast(RoomEventComposer.From(_event, _room!.Id, DateTimeOffset.UtcNow));
    }

    public async Task CancelEventAsync(PlayerData player, int eventId)
    {
        if (ActiveEvent is not { } running || running.Id != eventId || !MayRunEvent(player))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        await db.RoomEvents.Where(e => e.Id == eventId).ExecuteUpdateAsync(s => s.SetProperty(e => e.ExpiresAt, now));

        _event = null;
        Broadcast(new RoomEventCancelComposer());
    }

    public async Task EventChangedAsync()
    {
        await using (var db = await dbFactory.CreateDbContextAsync())
            await LoadEventAsync(db);

        if (_room is not null)
            Broadcast(RoomEventComposer.From(ActiveEvent, _room.Id, DateTimeOffset.UtcNow));
    }

    /// <summary>A promotion that ran out while people were here ends for them too.</summary>
    private void ExpireEvent()
    {
        if (_event is not null && ActiveEvent is null)
        {
            _event = null;
            Broadcast(new RoomEventCancelComposer());
        }
    }

    public Task StaffPickChangedAsync(bool staffPick)
    {
        if (_room is not null)
        {
            _room.IsStaffPick = staffPick;
            Broadcast(new RoomInfoUpdatedComposer(_room.Id));
        }

        return Task.CompletedTask;
    }

    public async Task ClickCharacterAsync(Guid playerId, int roomIndex)
    {
        if (_entities?.Find(playerId) is not { } clicker || _entities.FindByIndex(roomIndex) is not { } clicked || clicked == clicker)
            return;

        await WakeAsync(clicker);
        if (_entities.LookTo(clicker, clicked.X, clicked.Y) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));
    }

    public async Task RecordEntrySourceAsync(Guid playerId, string category, string position)
    {
        if (_entities?.Find(playerId) is null || _room is null)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomAdInteractions.Add(new RoomAdInteraction
        {
            UserId = playerId,
            Kind = RoomAdInteractionKind.PromotedEntry,
            EventId = ActiveEvent?.Id,
            RoomId = _room.Id,
            Source = category.Length > 64 ? category[..64] : category,
            Position = position.Length > 16 ? position[..16] : position,
        });
        await db.SaveChangesAsync();
    }
}
