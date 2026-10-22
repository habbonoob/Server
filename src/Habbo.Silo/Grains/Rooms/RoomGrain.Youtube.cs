using Habbo.Domain.Furniture.Youtube;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Video TVs: the hotel's playlists, one chosen per TV by those with rights, who step through it,
/// pause and carry on. Everyone in the room is shown the same.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What the client asks a TV to do.</summary>
    private const int PreviousVideo = 0;
    private const int NextVideo = 1;
    private const int PauseVideo = 2;
    private const int ContinueVideo = 3;

    /// <summary>What the client is told a TV's video is doing.</summary>
    private const int VideoPlaying = 1;
    private const int VideoPaused = 2;

    public async Task SendYoutubeDisplayAsync(Guid playerId, int itemId)
    {
        if (_items?.Find(itemId) is not { Interaction: YoutubeDisplayInteraction })
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var playlists = await db.YoutubePlaylists.AsNoTracking().OrderBy(p => p.OrderIndex).ToListAsync();
        var state = await db.YoutubeDisplayStates.AsNoTracking().FirstOrDefaultAsync(s => s.ItemId == itemId);
        SendTo(playerId, new YoutubeDisplayPlaylistsMessageComposer(itemId,
            playlists.Select(p => new YoutubeDisplayPlaylistData(p.Id, p.Title, p.Description)).ToList(), state?.PlaylistId ?? string.Empty));

        if (state is not null && await VideoAtAsync(db, state) is { } video)
            SendTo(playerId, VideoOf(itemId, video, state));
    }

    public async Task SetYoutubePlaylistAsync(Guid playerId, int itemId, string playlistId)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: YoutubeDisplayInteraction })
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.YoutubePlaylists.AnyAsync(p => p.Id == playlistId))
            return;

        var state = await db.YoutubeDisplayStates.FirstOrDefaultAsync(s => s.ItemId == itemId);
        if (state is null)
            db.YoutubeDisplayStates.Add(state = new YoutubeDisplayState { ItemId = itemId, PlaylistId = playlistId });
        (state.PlaylistId, state.Position, state.Paused) = (playlistId, 0, false);
        await db.SaveChangesAsync();

        if (await VideoAtAsync(db, state) is { } video)
            Broadcast(VideoOf(itemId, video, state));
    }

    public async Task ControlYoutubeAsync(Guid playerId, int itemId, int command)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: YoutubeDisplayInteraction })
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.YoutubeDisplayStates.FirstOrDefaultAsync(s => s.ItemId == itemId) is not { } state)
            return;

        switch (command)
        {
            case PreviousVideo or NextVideo:
                var count = await db.YoutubeVideos.CountAsync(v => v.PlaylistId == state.PlaylistId);
                if (count == 0)
                    return;
                state.Position = ((state.Position + (command == NextVideo ? 1 : -1)) % count + count) % count;
                state.Paused = false;
                await db.SaveChangesAsync();
                if (await VideoAtAsync(db, state) is { } video)
                    Broadcast(VideoOf(itemId, video, state));
                break;

            case PauseVideo or ContinueVideo:
                state.Paused = command == PauseVideo;
                await db.SaveChangesAsync();
                Broadcast(new YoutubeControlVideoMessageComposer(itemId, state.Paused ? VideoPaused : VideoPlaying));
                break;
        }
    }

    /// <summary>The video the TV is on, by its position in the playlist.</summary>
    private static Task<YoutubeVideo?> VideoAtAsync(Habbo.Infrastructure.AppDbContext db, YoutubeDisplayState state) =>
        db.YoutubeVideos.AsNoTracking().OrderBy(v => v.Position).Where(v => v.PlaylistId == state.PlaylistId)
            .Skip(state.Position).FirstOrDefaultAsync();

    private static YoutubeDisplayVideoMessageComposer VideoOf(int itemId, YoutubeVideo video, YoutubeDisplayState state) =>
        new(itemId, video.VideoId, video.StartAtSeconds, video.EndAtSeconds, state.Paused ? VideoPaused : VideoPlaying);
}
