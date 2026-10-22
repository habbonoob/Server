using Habbo.Protocol.Messages.Outgoing.Game.Lobby;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Resolution statues whose challenge a level reached meets: each is engraved with the level's badge, the
/// player's name and the date, wherever it stands, and the player is congratulated.</summary>
public sealed partial class PlayerGrain
{
    private async Task MeetResolutionsAsync(int achievementId, int level, string username)
    {
        foreach (var met in await resolutions.LevelReachedAsync(PlayerId, achievementId, level))
        {
            var engraved = BadgeEngraving.Of((int)AchievementResolutionState.Achieved, met.Badge, username, DateTimeOffset.UtcNow);
            await using var db = await dbFactory.CreateDbContextAsync();
            var statue = await db.UserItems.AsNoTracking().Include(i => i.Definition).FirstOrDefaultAsync(i => i.Id == met.ItemId);
            if (statue is null)
                continue;

            // a statue in a room is changed by the room, so everyone there sees it; one in an inventory, or that left
            // the room on the way, is changed where it is kept
            if (statue.RoomId is not { } roomId || !await GrainFactory.GetGrain<IGameRoomGrain>(roomId).ReplaceItemDataAsync(statue.Id, engraved))
                await FurniDataStore.SaveAsync(db, statue.Id, await FurniDataStore.LoadAsync(db, statue), engraved);

            Send(new AchievementResolutionCompletedMessageComposer(statue.Definition!.ClassName, met.Badge));
        }
    }
}
