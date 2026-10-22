using Habbo.Domain.Furniture;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends each of its bots walking to one of its furni, chosen at random: onto it, or as close as it gets.</summary>
[WiredBox("wf_act_bot_move")]
public sealed class BotMoveEffect : WiredEffect
{
    public override int Code => EffectCodes.BotMove;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots];

    public override Task RunAsync(WiredBox box, WiredRun run)
    {
        var targets = run.Furni(box).Where(item => item.Item.Definition!.Type == ItemType.Floor).ToList();
        if (targets.Count == 0)
            return Task.CompletedTask;

        foreach (var bot in WiredBots.Of(box, run))
        {
            var target = targets[run.Room.Random.Next(targets.Count)];
            run.Room.WalkTo(bot, target.X, target.Y);
        }

        return Task.CompletedTask;
    }
}
