using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Moderation;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Moderation;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>What staff do to a room and the people in it, and what the room keeps for them: who
/// came in and what was said.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Whether somebody who is staff: staff are never sent out of a room by staff.</summary>
    private bool IsStaff(RoomEntityStore.RoomEntity entity) => permissions.Has(entity.Player, PermissionNames.RoomStaffImmunity);

    public Task<bool> HasUserAsync(Guid playerId) => Task.FromResult(_entities?.Find(playerId) is not null);

    public Task<int> UserCountAsync() => Task.FromResult(_entities?.Count ?? 0);

    public async Task StaffKickAsync(Guid playerId)
    {
        if (_entities?.Find(playerId) is { } entity)
            await KickAsync(entity);
    }

    public async Task StaffMessageAsync(string message, bool caution, bool kick)
    {
        if (_entities is null)
            return;

        foreach (var entity in _entities.Entities.Where(e => !IsStaff(e)).ToList())
        {
            SendTo(entity.Player.Id, caution
                ? new ModeratorCautionComposer(message, string.Empty)
                : new ModeratorMessageComposer(message, string.Empty));
            if (kick)
                await KickAsync(entity);
        }
    }

    public async Task StaffModerateAsync(bool lockDoor, string? newName, bool kickUsers)
    {
        if (_room is null)
            return;

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.Rooms.FirstAsync(r => r.Id == _room.Id);
            if (lockDoor)
                row.State = _room.State = RoomState.Locked;
            if (newName is not null)
                row.Name = _room.Name = newName;
            await db.SaveChangesAsync();
        }

        if (kickUsers && _entities is not null)
        {
            foreach (var entity in _entities.Entities.Where(e => !IsStaff(e)).ToList())
                await KickAsync(entity);
        }
    }

    /// <summary>Keeps a line said here for the mod tool's chat logs.</summary>
    private async Task LogChatAsync(RoomEntityStore.RoomEntity entity, string text)
    {
        if (_room is null)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomChatLines.Add(new RoomChatLine
        {
            RoomId = _room.Id,
            UserId = entity.Player.Id,
            Text = text.Length > RoomChatLine.TextMaxLength ? text[..RoomChatLine.TextMaxLength] : text,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Keeps a visit here for the mod tool's room visits.</summary>
    private async Task LogVisitAsync(Guid playerId)
    {
        if (_room is null)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomVisits.Add(new RoomVisit { RoomId = _room.Id, UserId = playerId });
        await db.SaveChangesAsync();
    }
}
