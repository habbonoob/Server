using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Mutes each of its users in the room for the chosen minutes (0 lifts a mute), showing them
/// the box's message. The room's owner is never muted, nor staff.</summary>
[WiredBox("wf_act_mute_triggerer")]
public sealed class MuteUserEffect : WiredEffect
{
    public const int MaxMinutes = 10;

    public override int Code => EffectCodes.MuteUser;
    public override bool TargetsUsers => true;
    public override IReadOnlyList<int> DefaultIntParams => [1];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).Where(user => !run.Room.IsOwner(user) && user.ControllerLevel < RoomControllerLevels.Staff).ToList())
        {
            run.Room.Mute(user, Math.Clamp(box.IntParam(0), 0, MaxMinutes));
            if (!string.IsNullOrWhiteSpace(box.Settings.StringParam))
                run.Room.ShowMessage(user, run.Fill(box.Settings.StringParam), ShowMessageEffect.WiredBubbleStyle, toEveryone: false);
        }

        return Task.CompletedTask;
    }
}
