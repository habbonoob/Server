using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Bots;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Bots;

/// <summary>The bots players own, outside the rooms they stand in.</summary>
public sealed class BotService(IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger) : IBotService
{
    public async Task SendInventoryAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var bots = await db.Bots.AsNoTracking().Where(b => b.OwnerId == player.Id && b.RoomId == null).OrderBy(b => b.Id).ToListAsync();
        await messenger.SendAsync(player.Id, new BotInventoryComposer(bots.Select(BotData.From).ToList()));
    }
}
