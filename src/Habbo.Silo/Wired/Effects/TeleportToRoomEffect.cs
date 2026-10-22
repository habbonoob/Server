using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends each of its users through one of its furni that is a linked teleport, chosen at random: out of its
/// partner, in whatever room that stands in. The dialog has no settings of its own (Habbo's form writes none), so the
/// destination is the picked teleport's link; Habbo's own way of naming the room was not measured.</summary>
[WiredBox("wf_act_teleport_to_room")]
public sealed class TeleportToRoomEffect : WiredEffect
{
    public override int Code => EffectCodes.TeleportToRoom;
    public override bool PicksFurni => true;
    public override bool TargetsUsers => true;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var teleports = run.Furni(box).Where(item => item.Interaction is TeleportInteraction or HopperInteraction).ToList();
        if (teleports.Count == 0)
            return;

        foreach (var user in run.Users(box).ToList())
            await run.Room.SendThroughAsync(user, teleports[run.Room.Random.Next(teleports.Count)]);
    }
}
