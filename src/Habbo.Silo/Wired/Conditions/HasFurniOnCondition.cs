using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when other furni stand on top of the picked furni: on every one, or on any one,
/// as the dialog chooses.</summary>
[WiredBox("wf_cnd_has_furni_on")]
public sealed class HasFurniOnCondition : WiredCondition
{
    public override int Code => ConditionCodes.HasStackedFurnis;
    public override bool PicksFurni => true;
    // its radio: 0 any, 1 all (the client's requireall texts); a new box on Habbo: any
    private const int RequireAll = 1;
    private const int RequireAny = 0;

    public override IReadOnlyList<int> DefaultIntParams => [RequireAny];

    public override bool Passes(WiredBox box, WiredRun run) =>
        WiredQuantity.Holds(box.IntParam(0) == RequireAll, run.Furni(box), item => HasFurniOn(item, run.Room));

    /// <summary>Whether anything stands on the item: another floor item on one of its tiles, resting
    /// at or above its top.</summary>
    public static bool HasFurniOn(RoomItemStore.PlacedItem item, IWiredRoom room)
    {
        const double tolerance = 0.01;
        var top = RoomItemStore.TopOf(item);
        return RoomItemStore.Footprint(item).Any(tile =>
            room.Items.FloorItemsOn(tile.X, tile.Y).Any(other => other != item && other.Z >= top - tolerance));
    }
}
