using Habbo.Silo.Chests;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Counts, as its stack runs, the items its chests hold of the kinds its example furni show — all of them,
/// or only those the chests show in their preview — into a context variable of the run.</summary>
[WiredBox("wf_xtra_scan_chest_furni_by_type")]
public sealed class ChestItemTypeScannerAddon : WiredAddon
{
    private const int PreviewedOnly = 0;
    private const int ScanPreviewed = 1;

    public override int Code => AddonCodes.ChestItemTypeScanner;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks];
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public static async Task ScanAsync(WiredBox addon, WiredRun run)
    {
        if (WiredVariableUse.Variable(addon, run.Room) is not { CanWriteValue: true } variable)
            return;

        var kinds = run.Furni(addon, 0).Select(run.Room.Chests.KindOf).ToHashSet();
        var previewed = addon.IntParam(PreviewedOnly) == ScanPreviewed;
        var count = run.Furni(addon, 1).Sum(chest => run.Room.Chests.Count(chest, kinds, previewed));
        await run.Room.SetVariableAsync(variable, WiredHolder.Context, run, count);
    }
}
