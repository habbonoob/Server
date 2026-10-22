namespace Habbo.Silo.Wired.Effects;

/// <summary>Starts, stops, resets, pauses or resumes its counters.</summary>
[WiredBox("wf_act_control_clock")]
public sealed class ControlClockEffect : WiredEffect
{
    // clock_control.0 to .4
    private const int Start = 0;
    private const int Stop = 1;
    private const int Reset = 2;
    private const int Pause = 3;
    private const int Resume = 4;

    public override int Code => EffectCodes.ControlClock;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredGames.Clocks;
    public override IReadOnlyList<int> DefaultIntParams => [Start];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var games = run.Room.Games;
        foreach (var clock in run.Furni(box).Select(games.Clock).OfType<Rooms.GameClock>().ToList())
        {
            await (box.IntParam(0) switch
            {
                Stop => games.StopAsync(clock),
                Reset => games.ResetAsync(clock),
                Pause => games.PauseAsync(clock),
                Resume => games.ResumeAsync(clock),
                _ => games.StartAsync(clock),
            });
        }
    }
}
