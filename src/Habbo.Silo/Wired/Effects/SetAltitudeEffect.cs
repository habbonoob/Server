namespace Habbo.Silo.Wired.Effects;

/// <summary>Raises, lowers or sets the altitude of each of its furni (in hundredths, 0 to 80).</summary>
[WiredBox("wf_act_set_altitude")]
public sealed class SetAltitudeEffect : WiredEffect
{
    private const int Increase = 0;
    private const int Decrease = 1;
    private const double Hundredths = 100.0;

    public override int Code => EffectCodes.SetFurniAltitude;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, Increase];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var value = box.IntParam(0) / Hundredths;
        foreach (var item in run.Furni(box).ToList())
        {
            var altitude = box.IntParam(1) switch
            {
                Increase => item.Z + value,
                Decrease => item.Z - value,
                _ => value,
            };
            await run.Room.SetAltitudeAsync(item, altitude);
        }
    }
}
