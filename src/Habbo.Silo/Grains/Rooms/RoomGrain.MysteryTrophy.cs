using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Opening a mystery trophy box: it is gone, and one of the trophies it holds, drawn at random and
/// engraved with what its owner wrote, stands where it stood.</summary>
public sealed partial class RoomGrain
{
    private readonly Random _trophyRandom = new();

    public async Task OpenMysteryTrophyAsync(Guid playerId, int itemId, string inscription)
    {
        if (_items?.Find(itemId) is not { Interaction: MysteryTrophyInteraction } box || box.Item.OwnerId != playerId || box.Temporary
            || _entities?.Find(playerId) is not { } owner)
            return;

        var trophies = mysteryTrophyOptions.Value.Trophies;
        if (trophies.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var className = trophies[_trophyRandom.Next(trophies.Count)];
        if (await db.ItemDefinitions.FirstOrDefaultAsync(d => d.ClassName == className) is not { } definition)
            return;

        var (x, y, rotation) = (box.X, box.Y, box.Rotation);
        if (!await _furniture!.RemoveAsync(box))
            return;

        var engraving = (await wordFilter.FilterAsync(inscription, _filterWords)).Text;
        var trophy = new UserItem { OwnerId = playerId, DefinitionId = definition.Id };
        db.UserItems.Add(trophy);
        interactions.For(definition).Grant(new FurniGrant(db, trophy, definition, string.Empty, engraving, owner.Player.Username, DateTime.UtcNow));
        await db.SaveChangesAsync();

        await PlaceFloorItemAsync(playerId, trophy.Id, x, y, rotation);
        if (_items.Find(trophy.Id) is null)
            await GrainFactory.GetGrain<IPlayerGrain>(playerId).AnnounceItemsAsync([trophy.Id]);
    }
}
