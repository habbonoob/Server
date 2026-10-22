namespace Habbo.Silo.Wired.Effects;

/// <summary>Adds time to its counters, takes it away, or sets it (seconds, minutes and the half
/// second between, then the operation: increase, decrease, set).</summary>
[WiredBox("wf_act_adjust_clock")]
public sealed class AdjustClockEffect : WiredEffect
{
    private const int Increase = 0;
    private const int Decrease = 1;

    public override int Code => EffectCodes.AdjustClock;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredGames.Clocks;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, Increase];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var amount = WiredGames.Pulses(box.IntParam(0), box.IntParam(1), box.IntParam(2));
        var games = run.Room.Games;
        foreach (var clock in run.Furni(box).Select(games.Clock).OfType<Rooms.GameClock>().ToList())
        {
            var pulses = box.IntParam(3) switch
            {
                Increase => clock.Pulses + amount,
                Decrease => clock.Pulses - amount,
                _ => amount,
            };
            await games.SetTimeAsync(clock, pulses);
        }
    }
}
