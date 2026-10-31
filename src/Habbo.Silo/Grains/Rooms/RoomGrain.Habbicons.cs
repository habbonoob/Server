using Habbo.Protocol.Messages.Outgoing.Room.Engine;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Habbicons shown in the room: a bubble over the avatar, which everyone in the room sees (the one who sent
/// it as well: their client draws it only when the room says so).</summary>
public sealed partial class RoomGrain
{
    public async Task UseHabbiconAsync(Guid playerId, int habbiconId)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        await WakeAsync(entity);
        ShowHabbicon(entity, habbiconId);
        TrackAction(entity, Habbo.Silo.RewardTracks.RewardTrackActions.UseHabbicon);
        if (_wired is not null)
            await _wired.HabbiconUsedAsync(entity, habbiconId);
    }
}
