namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user performs the chosen action: waves, sits, holds up a sign (any, or
/// the chosen one), dances, falls asleep, and so on.</summary>
[WiredBox("wf_trg_user_performs_action")]
public sealed class PerformActionTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.PerformAction;
    public override IReadOnlyList<int> DefaultIntParams => [WiredUserActions.Wave];

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.UserPerformedAction, Action: { } action }
        && action == box.IntParam(0)
        && (box.Settings.StringParam.Length == 0 || box.Settings.StringParam == (e.Text ?? string.Empty));
}
