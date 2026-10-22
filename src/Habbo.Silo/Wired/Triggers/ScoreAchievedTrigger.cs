namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a team's score reaches the box's points (1 to 1000): the chosen team, or any.</summary>
[WiredBox("wf_trg_score_achieved")]
public sealed class ScoreAchievedTrigger : WiredTrigger
{
    private const int Points = 0;
    private const int Team = 1;

    public override int Code => TriggerCodes.ScoreAchieved;
    public override IReadOnlyList<int> DefaultIntParams => [100, WiredGames.AnyOrTriggererTeam];

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.ScoreChanged, Team: { } team, OldScore: { } old, NewScore: { } now }
        && (box.IntParam(Team) == WiredGames.AnyOrTriggererTeam || box.IntParam(Team) == team)
        && old < box.IntParam(Points, 1) && now >= box.IntParam(Points, 1);
}
