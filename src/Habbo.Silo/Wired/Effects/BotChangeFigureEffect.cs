namespace Habbo.Silo.Wired.Effects;

/// <summary>Dresses each of its bots in the look the box was saved with (the dialog copies its owner's), and the bot
/// keeps it.</summary>
[WiredBox("wf_act_bot_clothes")]
public sealed class BotChangeFigureEffect : WiredEffect
{
    public override int Code => EffectCodes.BotChangeFigure;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots];

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var figure = WiredBots.Rest(box).Trim();
        if (figure.Length == 0)
            return;

        foreach (var bot in WiredBots.Of(box, run))
            await run.Room.DressBotAsync(bot, figure);
    }
}
