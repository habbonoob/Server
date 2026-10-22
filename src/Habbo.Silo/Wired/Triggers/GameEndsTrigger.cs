namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when the room's game ends: its countdown ran out, or its clock was stopped.</summary>
[WiredBox("wf_trg_game_ends")]
public sealed class GameEndsTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.GameEnds;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) => e.Type == WiredEventType.GameEnded;
}
