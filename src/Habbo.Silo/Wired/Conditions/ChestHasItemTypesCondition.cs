using Habbo.Silo.Chests;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the items its chests hold of the kinds its example furni show compare as chosen with a
/// number or a variable's value.</summary>
[WiredBox("wf_cnd_chest_has_item_type")]
public sealed class ChestHasItemTypesCondition : ChestHasItemsCondition
{
    public override int Code => ConditionCodes.ChestHasItemTypes;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.PickedFurni2), WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];

    protected override int ChestSlot => 1;
    protected override int ReferenceFurniSlot => 2;

    protected override IReadOnlySet<ChestKind>? Kinds(WiredBox box, WiredRun run) =>
        run.Furni(box, 0).Select(run.Room.Chests.KindOf).ToHashSet();
}
