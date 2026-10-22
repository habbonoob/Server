using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Shows each of its users the box's message, then sends them out of the room. The room's
/// owner is never kicked from their own room, nor staff from any.</summary>
[WiredBox("wf_act_kick_user")]
public sealed class KickUserEffect : WiredEffect
{
    public override int Code => EffectCodes.KickFromRoom;
    public override bool TargetsUsers => true;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        foreach (var user in run.Users(box).Where(user => !run.Room.IsOwner(user) && user.ControllerLevel < RoomControllerLevels.Staff).ToList())
        {
            if (!string.IsNullOrWhiteSpace(box.Settings.StringParam))
                run.Room.ShowMessage(user, run.Fill(box.Settings.StringParam), ShowMessageEffect.WiredBubbleStyle, toEveryone: false);
            await run.Room.KickAsync(user);
        }
    }
}
