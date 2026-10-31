using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Crackables (bonus bags): each hit is shown, and the last cracks it open as habbo.com does (October 2026):
/// the bag shows itself cracked, goes after a moment, and the prize it held stands where it stood, its owner's, with
/// a notice to the one who opened it.</summary>
public sealed partial class RoomGrain
{
    /// <summary>How long a cracked crackable stays in sight before it goes (habbo.com's removal delay).</summary>
    private const int CrackedRemovalDelayMilliseconds = 1592;

    private const string RewardReceivedNotice = "reward_received";
    private const string RewardReceivedMessage = "${wiredfurni.rewardsuccess.body}";

    private CrackableCatalog Crackables => ServiceProvider.GetRequiredService<CrackableCatalog>();

    /// <summary>A hit on a crackable; false when the item is none.</summary>
    private async Task<bool> CrackAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (item.Interaction is not CrackableInteraction)
            return false;
        if (!IsController(user.Player.Id) || item.Temporary)
            return true;

        // a bag given before bags could crack holds no count yet: whole, of the bonus bag's hits
        var target = item.Data.Target is > 0 and var set ? set : CrackableInteraction.BonusBagHits;
        var hits = (item.Data.Hits ?? 0) + 1;
        var counted = CrackableInteraction.Whole(target) with { Hits = Math.Min(hits, target) };
        if (hits < target)
        {
            await _furniture!.UpdateAsync(item, counted);
            return true;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var prizes = await Crackables.PrizesOfAsync(item.Item.Definition!.ClassName);
        var prizeName = prizes.Count == 0 ? null : prizes[Random.Shared.Next(prizes.Count)];
        if (prizeName is null || await db.ItemDefinitions.FirstOrDefaultAsync(d => d.ClassName == prizeName && d.Type == ItemType.Floor) is not { } definition)
        {
            await _furniture!.UpdateAsync(item, counted);
            return true;
        }

        var (x, y, rotation, ownerId) = (item.X, item.Y, item.Rotation, item.Item.OwnerId);
        await _furniture!.UpdateAsync(item, counted with { State = CrackableInteraction.CrackedState }, persist: false);
        if (!await ((RoomFurnitureUse)_furniture).RemoveAsync(item, CrackedRemovalDelayMilliseconds))
            return true;

        var prize = new UserItem
        {
            OwnerId = ownerId, DefinitionId = definition.Id, Definition = definition, RoomId = _room!.Id, X = x, Y = y,
            Z = _items!.SurfaceAt(x, y), Rotation = rotation,
        };
        db.UserItems.Add(prize);
        interactions.For(definition).Grant(new FurniGrant(db, prize, definition, string.Empty, string.Empty, string.Empty, DateTime.UtcNow));
        await db.SaveChangesAsync();
        ShowPlacedFloorItem(prize, await furniData.LoadAsync(db, prize));
        SendTo(user.Player.Id, new NotificationDialogMessageComposer(RewardReceivedNotice,
            [new NotificationParameterData("message", RewardReceivedMessage)]));
        return true;
    }
}
