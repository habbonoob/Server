namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its counters (all or any of them, as the quantifier says) show a time lower than, equal to or
/// higher than the box's (seconds, minutes, half second, comparison).</summary>
[WiredBox("wf_cnd_counter_time_matches")]
public sealed class ClockTimeMatchesCondition : WiredCondition
{
    public override int Code => ConditionCodes.ClockTimeMatches;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredGames.Clocks;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0];
    public override byte QuantifierType => WiredQuantifierTypes.Furni;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var time = WiredGames.Pulses(box.IntParam(0), box.IntParam(1), box.IntParam(2));
        var clocks = run.Furni(box).Select(run.Room.Games.Clock).OfType<Rooms.GameClock>().ToList();
        return WiredQuantity.Passes(box, IsNegative, clocks, clock => WiredGames.Compare(clock.Pulses, box.IntParam(3), time));
    }
}
