namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when the bot the box names ends a walk on one of its picked furni.</summary>
[WiredBox("wf_trg_bot_reached_stf")]
public sealed class BotReachedFurniTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.BotDestinationReached;
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.NamedBot];
    public override bool PicksFurni => true;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.BotReachedFurni, Bot: { } bot, Item: { } item }
        && WiredBots.Listens(box, bot, e, room) && Watches(box, item, e, room);
}
