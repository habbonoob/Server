using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a link message arrives on one of its WIRED Links. The stack reads the message as the placeholder
/// $(trigger_text), and the room it came from as @event.link.source_room_id.</summary>
[WiredBox("wf_trg_receive_message")]
public sealed class ReceiveLinkMessageTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.ReceiveLinkMessage;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredLinks.PickRule;

    public override WiredValidation? Validate(FurniWired settings) => WiredLinks.RequireLinks(settings);

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.LinkMessageReceived, Item: { } link } && Watches(box, link, e, room);
}
