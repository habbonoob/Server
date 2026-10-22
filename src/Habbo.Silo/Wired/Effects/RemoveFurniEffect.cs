namespace Habbo.Silo.Wired.Effects;

/// <summary>Takes its furni out of the room, if they are temporary copies Place Furni put there: nothing else (Habbo:
/// vault_zero, temporary_selection_cleanup). By default the selector's.</summary>
[WiredBox("wf_act_remove_furni")]
public sealed class RemoveFurniEffect : WiredEffect
{
    public override int Code => EffectCodes.RemoveFurni;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Unpicked];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var copy in run.Furni(box).Where(item => item.Temporary).ToList())
            await run.Room.RemoveCopyAsync(copy);
    }
}
