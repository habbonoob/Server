using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Commands;

/// <summary>:empty, :emptypets and :emptybots — deletes what a player keeps in their inventory, once they confirm.
/// Only what is in the inventory itself: nothing placed in a room, for sale in the marketplace, in a chest or holding
/// one.</summary>
public sealed class InventoryCommandService(
    IDbContextFactory<AppDbContext> dbFactory,
    PlayerMessenger messenger,
    IPetService pets,
    IBotService bots)
{
    /// <summary>The word after the command that confirms it.</summary>
    public const string Confirmation = "yes";

    /// <summary>Tells how much would go without the confirmation, and deletes it with; what to tell the player.</summary>
    public async Task<string> EmptyAsync(PlayerData player, InventoryPart part, string argument)
    {
        var confirmed = argument.Trim().Equals(Confirmation, StringComparison.OrdinalIgnoreCase);
        await using var db = await dbFactory.CreateDbContextAsync();
        var furni = db.UserItems.Where(i => i.OwnerId == player.Id && i.RoomId == null && !i.InMarketplace && i.ChestId == null
                                            && !db.UserItems.Any(stored => stored.ChestId == i.Id)
                                            && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0));
        var petRows = db.Pets.Where(p => p.OwnerId == player.Id && p.RoomId == null);
        var botRows = db.Bots.Where(b => b.OwnerId == player.Id && b.RoomId == null);
        var (command, what) = part switch
        {
            InventoryPart.Furni => ("empty", "furni"),
            InventoryPart.Pets => ("emptypets", "pets"),
            _ => ("emptybots", "bots"),
        };

        if (!confirmed)
        {
            var count = part switch
            {
                InventoryPart.Furni => await furni.CountAsync(),
                InventoryPart.Pets => await petRows.CountAsync(),
                _ => await botRows.CountAsync(),
            };
            return count == 0
                ? $"You have no {what} in your inventory."
                : $"This deletes the {count} {what} in your inventory for good. Say :{command} {Confirmation} to do it.";
        }

        int deleted;
        switch (part)
        {
            case InventoryPart.Furni:
                deleted = await furni.ExecuteDeleteAsync();
                await messenger.SendAsync(player.Id, new FurniListInvalidateComposer());
                break;
            case InventoryPart.Pets:
                deleted = await petRows.ExecuteDeleteAsync();
                await pets.SendInventoryAsync(player);
                break;
            default:
                deleted = await botRows.ExecuteDeleteAsync();
                await bots.SendInventoryAsync(player);
                break;
        }

        return $"{deleted} {what} deleted.";
    }
}
