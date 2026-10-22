using Habbo.Domain.Furniture;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Puts each of its bots onto one of its furni, chosen at random; onto furni no one may stand on, next to it.</summary>
[WiredBox("wf_act_bot_teleport")]
public sealed class BotTeleportEffect : WiredEffect
{
    public override int Code => EffectCodes.BotTeleport;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var targets = run.Furni(box).Where(item => item.Item.Definition!.Type == ItemType.Floor).ToList();
        if (targets.Count == 0)
            return Task.CompletedTask;

        foreach (var bot in WiredBots.Of(box, run))
        {
            if (TeleportToFurniEffect.Landing(targets[run.Room.Random.Next(targets.Count)], run.Room.Items) is var (x, y))
                run.Room.WarpUser(bot, x, y);
        }

        return Task.CompletedTask;
    }
}
