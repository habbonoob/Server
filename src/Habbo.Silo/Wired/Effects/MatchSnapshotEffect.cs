namespace Habbo.Silo.Wired.Effects;

/// <summary>Puts the picked furni back as they stood when the box was saved: their state, which way
/// they faced and where, as the dialog's checkboxes choose. A furni that cannot stand where it was
/// (something is in the way) keeps its place.</summary>
[WiredBox("wf_act_match_to_sshot")]
public sealed class MatchSnapshotEffect : WiredEffect
{
    public override int Code => EffectCodes.SetFurniStateTo;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override bool TakesSnapshot => true;

    /// <summary>State, direction and position checked; altitude, the fourth, not.</summary>
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>Only the snapshotted furni its furni source supplies: by default its picks, which are all of them.</summary>
    public override Task RunAsync(WiredBox box, WiredRun run) =>
        Restore(box, run.Room, run, run.Furni(box).Select(item => item.Item.Id).ToHashSet());

    /// <summary>Also what the dialog's "apply" button does, for every snapshotted furni.</summary>
    public static async Task Restore(WiredBox box, IWiredRoom room, WiredRun? run = null, IReadOnlySet<int>? only = null)
    {
        var parts = WiredFurni.SnapshotParts.Of(box);
        foreach (var snapshot in box.Settings.Snapshots)
        {
            if (only?.Contains(snapshot.ItemId) == false || room.FindItem(snapshot.ItemId) is not { } item)
                continue;

            if (parts.State && WiredFurni.HasStates(item) && item.Data.StateText != snapshot.State)
                await room.SetStateAsync(item, snapshot.State);

            var x = parts.Position ? snapshot.X : item.X;
            var y = parts.Position ? snapshot.Y : item.Y;
            var rotation = parts.Direction ? snapshot.Rotation : item.Rotation;
            if (x != item.X || y != item.Y || rotation != item.Rotation)
                await (run is not null ? run.MoveAsync(item, x, y, rotation) : room.MoveItemAsync(item, x, y, rotation, WiredMoveOptions.Default));

            if (parts.Altitude && Math.Abs(item.Z - snapshot.Z) > WiredFurni.AltitudeTolerance)
                await room.SetAltitudeAsync(item, snapshot.Z);
        }
    }
}
