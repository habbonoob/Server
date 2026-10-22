using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when the chosen variable is given, changed (up, down, or to the same value, as
/// the dialog chooses), or taken, by whichever of wired here, wired in another room, the inspection or the web the
/// dialog allows; the holder is what the stack runs for.</summary>
[WiredBox("wf_trg_var_changed")]
public sealed class VariableChangedTrigger : WiredTrigger
{
    private const int OnCreated = 0;
    private const int OnChanged = 1;
    private const int OnDeleted = 2;
    private const int ChangeMask = 3;
    private const int Origins = 4;

    // trigger_options.1.N, a bit each
    private const int Increased = 1 << 0;
    private const int Decreased = 1 << 1;
    private const int Unchanged = 1 << 2;

    public override int Code => TriggerCodes.VariableUpdate;
    // where changes may come from: every option by default (Habbo's is not known), as for a box saved before there
    // was a choice
    public override IReadOnlyList<int> DefaultIntParams => [1, 1, 0, Increased | Decreased | Unchanged, WiredVariableOrigins.Every];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room)
    {
        if (e.Type != WiredEventType.VariableChanged || box.Settings.VariableIds.Count == 0
            || e.VariableId != box.Settings.VariableIds[0])
            return false;

        // Habbo: a box allowing only this room's wired ignored the inspection, and one allowing the inspection heard it
        var origins = box.IntParam(Origins, WiredVariableOrigins.Every);
        if (origins != WiredVariableOrigins.Every && (origins & e.VariableOrigin) == 0)
            return false;

        return e.VariableChange switch
        {
            WiredVariableChange.Created => box.Flag(OnCreated),
            WiredVariableChange.Deleted => box.Flag(OnDeleted),
            WiredVariableChange.Changed when box.Flag(OnChanged) => (box.IntParam(ChangeMask) & Direction(e)) != 0,
            _ => false,
        };
    }

    private static int Direction(WiredEvent e) =>
        e.NewValue > e.OldValue ? Increased : e.NewValue < e.OldValue ? Decreased : Unchanged;
}
