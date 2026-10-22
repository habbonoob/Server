using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Writes the box's message to the room's wired log, at the chosen level.</summary>
[WiredBox("wf_act_log")]
public class WriteLogEffect : WiredEffect
{
    public override int Code => EffectCodes.WriteToLogs;
    public override IReadOnlyList<int> DefaultIntParams => [WiredLogLevels.Info];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var level = (byte)Math.Clamp(box.IntParam(0), WiredLogLevels.Info, WiredLogLevels.Debug);
        return run.Room.LogAsync(level, WiredLogLevels.WiredSource, run.Fill(box.Settings.StringParam));
    }
}
