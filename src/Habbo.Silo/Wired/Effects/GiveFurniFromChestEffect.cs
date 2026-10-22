using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives furniture from its furni chests into the users' inventories, picked in the order the box says:
/// the chest's random order, oldest first, or newest first.</summary>
[WiredBox("wf_act_give_furni")]
public sealed class GiveFurniFromChestEffect : GiveFromChestEffect
{
    public override int Code => EffectCodes.GiveFurniFromChest;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 1, 0, WiredVariableTargets.Context, 1, 0];

    protected override Task GiveAsync(WiredBox box, WiredRun run, List<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user,
        int? amount, WiredChestGift gift) =>
        run.Room.Chests.GiveItemsAsync(chests, user, amount, box.IntParam(Extra), gift);
}
