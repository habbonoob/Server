using Habbo.Domain.Furniture;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Places temporary copies of its furni: by default of those its snapshot saw, where and as they stood when it
/// saw them (Habbo: vault_zero, temporary_snapshot_copy). A copy goes where its furni stood, or onto a custom target (a
/// furni, or a user), moved by the offsets; on top of what is there, at its furni's altitude, or at the target's, raised
/// by the altitude offset. It may be given a furni variable as it comes. Only Remove Furni takes copies away.</summary>
[WiredBox("wf_act_place_furni")]
public sealed class PlaceFurniEffect : WiredEffect
{
    // the dialog's fields, in its order
    private const int CustomTargetIsUser = 0;
    private const int LocationMode = 1;
    private const int AltitudeMode = 2;
    private const int XOffset = 3;
    private const int YOffset = 4;
    private const int AltitudeOffset = 5;
    private const int SpawnWithVariable = 6;
    private const int SpawnValueKind = 7;
    private const int SpawnValue = 8;
    private const int SpawnValueTarget = 9;

    private const int CustomLocation = 1;
    private const int OnTop = 0;
    private const int SourceAltitude = 1;
    private const int CustomAltitude = 2;
    private const int FromVariable = 1;

    // furni slots: what to copy, the custom target, the spawn value's furni; users merged with the last two
    private const int Copied = 0;
    private const int TargetFurni = 1;
    private const int ValueFurni = 2;
    private const int TargetUsers = 0;
    private const int ValueUsers = 1;

    private const int SpawnVariable = 0;
    private const int SpawnValueVariable = 1;

    private const double Hundredths = 100.0;

    public override int Code => EffectCodes.PlaceFurni;
    public override bool AllowsWallFurni => true;
    public override bool TakesSnapshot => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.Placed, WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni), WiredSlot.EitherPicks.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0, 0, 0, 0, 0, 0, WiredVariableTargets.Context];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var custom = box.IntParam(LocationMode) == CustomLocation || box.IntParam(AltitudeMode) == CustomAltitude;
        (int X, int Y, double Z)? target = null;
        if (custom)
        {
            target = box.Flag(CustomTargetIsUser)
                ? run.Users(box, TargetUsers).Select(user => ((int, int, double)?)(user.X, user.Y, user.Z)).FirstOrDefault()
                : run.Furni(box, TargetFurni).Select(item => ((int, int, double)?)(item.X, item.Y, item.Z)).FirstOrDefault();
            if (target is null)
                return;
        }

        foreach (var (template, data, at) in Copies(box, run))
        {
            var (x, y) = box.IntParam(LocationMode) == CustomLocation ? (target!.Value.X, target.Value.Y) : (at.X, at.Y);
            x += box.IntParam(XOffset);
            y += box.IntParam(YOffset);
            var offset = box.IntParam(AltitudeOffset) / Hundredths;
            double? z = box.IntParam(AltitudeMode) switch
            {
                SourceAltitude => at.Z + offset,
                CustomAltitude => target!.Value.Z + offset,
                _ => offset == 0 ? null : run.Room.Items.SurfaceAt(x, y) + offset,
            };

            if (await run.Room.PlaceCopyAsync(template, data, x, y, z, at.Rotation) is { } copy && box.Flag(SpawnWithVariable))
                await GiveSpawnVariableAsync(box, run, copy);
        }
    }

    /// <summary>What to copy, as what and from where: a snapshot's furni as it saw them, anything else as it is now.</summary>
    private static IEnumerable<(RoomItemStore.PlacedItem Template, Habbo.Silo.Abstractions.Furni.FurniDataView Data, (int X, int Y, double Z, int Rotation) At)>
        Copies(WiredBox box, WiredRun run)
    {
        if (box.FurniSource(Copied) == WiredSources.SnapshotFurni)
        {
            foreach (var snapshot in box.Settings.Snapshots)
            {
                if (run.Room.FindItem(snapshot.ItemId) is { } template && template.Item.Definition!.Type == ItemType.Floor)
                    yield return (template, template.Data with { State = snapshot.State }, (snapshot.X, snapshot.Y, snapshot.Z, snapshot.Rotation));
            }

            yield break;
        }

        foreach (var item in run.Furni(box, Copied).Where(item => item.Item.Definition!.Type == ItemType.Floor).ToList())
            yield return (item, item.Data, (item.X, item.Y, item.Z, item.Rotation));
    }

    private static async Task GiveSpawnVariableAsync(WiredBox box, WiredRun run, RoomItemStore.PlacedItem copy)
    {
        if (WiredVariableUse.Variable(box, run.Room, SpawnVariable) is not { Target: WiredVariableTargets.Furni } variable)
            return;

        int? value = box.IntParam(SpawnValueKind) != FromVariable ? box.IntParam(SpawnValue)
            : WiredVariableUse.Variable(box, run.Room, SpawnValueVariable) is not { } from ? null
            : SpawnValueHolders(box, run).Select(holder => run.Room.VariableValue(from, holder, run)).FirstOrDefault(v => v is not null);
        if (value is { } given)
            await run.Room.GiveVariableAsync(variable, WiredHolder.Of(copy), run, given, overrideExisting: true);
    }

    /// <summary>Whose value of the variable the copy is given: the spawn value's merged slot, by its target.</summary>
    private static IEnumerable<WiredHolder> SpawnValueHolders(WiredBox box, WiredRun run) => box.IntParam(SpawnValueTarget) switch
    {
        WiredVariableTargets.Furni => run.Furni(box, ValueFurni).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.User => run.Users(box, ValueUsers).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.Global => [WiredHolder.Room],
        WiredVariableTargets.Context => [WiredHolder.Context],
        _ => [],
    };
}
