using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Has each of its bots say the box's message to the room, or shout it, in the bubble width the dialog chose
/// (-1 for the room's). The message's placeholders are filled in.</summary>
[WiredBox("wf_act_bot_talk")]
public sealed class BotTalkEffect : WiredEffect
{
    private const int Mode = 0;
    private const int BubbleWidth = 1;
    private const int Shout = 1;

    public override int Code => EffectCodes.BotTalk;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots];
    public override IReadOnlyList<int> DefaultIntParams => [0, -1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var text = run.Fill(WiredBots.Rest(box));
        if (string.IsNullOrWhiteSpace(text))
            return;

        var kind = box.IntParam(Mode) == Shout ? RoomChatKind.Shout : RoomChatKind.Talk;
        foreach (var bot in WiredBots.Of(box, run))
            await run.Room.BotSayAsync(bot, text, kind, box.IntParam(BubbleWidth, -1), null);
    }
}
