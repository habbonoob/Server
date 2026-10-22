namespace Habbo.Silo.Wired.Effects;

/// <summary>Sets what its users' clicks do in the room: on users, as usual, clicking the user but walking behind them,
/// or passing through them; on furni, as usual or passing through. Their client keeps it until they leave.</summary>
[WiredBox("wf_act_click_conf")]
public sealed class ClickConfigurationEffect : WiredEffect
{
    private const int UserClick = 0;
    private const int FurniClick = 1;

    // the dialog's choices
    private const int MaxUserOption = 2;
    private const int MaxFurniOption = 1;

    public override int Code => EffectCodes.ClickConfiguration;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var userOption = Math.Clamp(box.IntParam(UserClick), 0, MaxUserOption);
        var furniOption = Math.Clamp(box.IntParam(FurniClick), 0, MaxFurniOption);
        foreach (var user in run.Users(box).ToList())
            run.Room.SetClickSettings(user, userOption, furniOption);
        return Task.CompletedTask;
    }
}
