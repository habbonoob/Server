using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Has each of its bots say the box's message to its users only, talked or whispered, in the bubble width the
/// dialog chose (-1 for the room's). The message's placeholders are filled in.</summary>
[WiredBox("wf_act_bot_talk_to_avatar")]
public sealed class BotTalkToUsersEffect : WiredEffect
{
    private const int Mode = 0;
    private const int BubbleWidth = 1;
    private const int Whisper = 1;
    private const int Listeners = 1;

    public override int Code => EffectCodes.BotTalkDirectToAvatar;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Bots, WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [0, -1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        var text = run.Fill(WiredBots.Rest(box));
        var listeners = run.Users(box, Listeners).Where(user => user.Bot is null && user.Pet is null).ToList();
        if (string.IsNullOrWhiteSpace(text) || listeners.Count == 0)
            return;

        var kind = box.IntParam(Mode) == Whisper ? RoomChatKind.Whisper : RoomChatKind.Talk;
        foreach (var bot in WiredBots.Of(box, run))
            await run.Room.BotSayAsync(bot, text, kind, box.IntParam(BubbleWidth, -1), listeners);
    }
}
