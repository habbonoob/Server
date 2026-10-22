namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when the bot the box names ends a walk next to a user; that user is who the stack runs for.</summary>
[WiredBox("wf_trg_bot_reached_avtr")]
public sealed class BotReachedUserTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.BotAvatarReached;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.NamedBot];

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.BotReachedUser, Bot: { } bot } && WiredBots.Listens(box, bot, e, room);
}
