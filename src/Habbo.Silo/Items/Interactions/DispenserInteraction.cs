using System.Globalization;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Furni that hands something out: the user walks up to its front, the furni works for a moment, and the
/// user is given one of the things its definition lists (<see cref="Domain.Furniture.ItemDefinition.VendingIds"/>),
/// at random. What reaches the avatar, a hand item or an effect, is the subclass's.</summary>
public abstract class DispenserInteraction : FurniInteraction
{
    private const int IdleState = 0;
    private const int WorkingState = 1;

    /// <summary>How long the furni works before the user has what it gives.</summary>
    private static readonly TimeSpan VendDuration = TimeSpan.FromMilliseconds(1000);

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    /// <summary>Gives the user what the furni dispensed.</summary>
    protected abstract void Give(FurniUse use, int id);

    public override Task UseAsync(FurniUse use)
    {
        var ids = use.Item.Item.Definition!.VendingIds;
        if (ids.Count == 0 || use.Item.IsBusy)
            return Task.CompletedTask;

        // the tile in front of the furni, the way it faces
        var (dx, dy) = RoomEntityStore.Offset(use.Item.Rotation);
        var (frontX, frontY) = (use.Item.X + dx, use.Item.Y + dy);
        var target = use.Room.IsWithinReach(use.User, use.Item) ? (use.User.X, use.User.Y) : (frontX, frontY);

        return use.Room.WalkThenAsync(use.User, target.Item1, target.Item2, async () =>
        {
            if (!use.Room.IsPlaced(use.Item) || !use.Room.IsWithinReach(use.User, use.Item) || use.Item.IsBusy)
                return;

            use.Item.BusyUntil = DateTime.UtcNow + VendDuration;
            use.Room.LookAt(use.User, use.Item.X, use.Item.Y);
            // working is only shown, and on the item's own data, so a limited edition keeps its number and one picked up
            // mid-vend is not stored working
            await use.Room.UpdateAsync(use.Item, use.Item.Data with { State = WorkingState.ToString(CultureInfo.InvariantCulture) }, persist: false);
            use.Room.Schedule(VendDuration, async () =>
            {
                if (!use.Room.IsPlaced(use.Item))
                    return;

                await use.Room.UpdateAsync(use.Item, use.Item.Data with { State = IdleState.ToString(CultureInfo.InvariantCulture) }, persist: false);
                Give(use, ids[Random.Shared.Next(ids.Count)]);
            });
        });
    }
}
