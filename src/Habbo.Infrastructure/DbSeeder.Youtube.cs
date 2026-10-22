using Habbo.Domain.Furniture.Youtube;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The playlists the hotel's video TVs offer.</summary>
public static partial class DbSeeder
{
    private sealed record SeedYoutube(List<SeedYoutubePlaylist>? Playlists);

    private sealed record SeedYoutubePlaylist(string Id, string Title, string Description, List<SeedYoutubeVideo>? Videos);

    private sealed record SeedYoutubeVideo(string VideoId, int StartAt, int EndAt);

    /// <summary>seed/youtube_playlists.json. A playlist the hotel already has keeps whatever it was changed to.</summary>
    private static async Task SeedYoutubeAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedYoutube>("youtube_playlists.json", ct);
        var known = await db.YoutubePlaylists.Select(p => p.Id).ToListAsync(ct);
        var order = known.Count;
        var added = 0;
        foreach (var playlist in (file?.Playlists ?? []).Where(p => !known.Contains(p.Id)))
        {
            var position = 0;
            db.YoutubePlaylists.Add(new YoutubePlaylist
            {
                Id = playlist.Id, Title = playlist.Title, Description = playlist.Description, OrderIndex = order++,
                Videos = (playlist.Videos ?? []).Select(v => new YoutubeVideo
                {
                    PlaylistId = playlist.Id, Position = position++, VideoId = v.VideoId, StartAtSeconds = v.StartAt, EndAtSeconds = v.EndAt,
                }).ToList(),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} video playlist(s) from {File}.", added, "youtube_playlists.json");
        }
    }
}
