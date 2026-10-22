namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a game starts in the room: a clock that was reset is started.</summary>
[WiredBox("wf_trg_game_starts")]
public sealed class GameStartsTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.GameStarts;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) => e.Type == WiredEventType.GameStarted;
}
