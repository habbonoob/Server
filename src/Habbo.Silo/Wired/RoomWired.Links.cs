using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>WIRED Links (wf_link): what the room's send-link effects send through them, and what arrives on them. The
/// grain finds where a Link leads — itself, when it receives its own room's, and the Links of its network in other
/// rooms — and hands back what reaches this room's Links.</summary>
public sealed partial class RoomWired
{
    /// <summary>How many Links a message passes through, each receiving stack sending it on, before it is dropped. Habbo's
    /// own bound is unmeasured; one is needed, or two rooms that answer each other would never stop.</summary>
    public const int MaxLinkHops = 16;

    public async Task SendLinkAsync(RoomItemStore.PlacedItem link, bool dataframe, string message, IReadOnlyList<WiredLinkRow> rows,
        WiredRun from)
    {
        // once the pulse has run all the stacks it may, what arrives would only have every stack refuse it again
        if (RunsSpent || from.Execution.Stopped)
            return;

        var hops = from.Event.Type is WiredEventType.LinkMessageReceived or WiredEventType.LinkDataframeReceived
            ? from.Event.LinkHops + 1
            : 0;
        if (hops > MaxLinkHops)
        {
            Record(WiredErrors.Recursion);
            return;
        }

        FlashItem(link);
        await actions.SendLink(link, new WiredLinkPayload(RoomId, dataframe, message, rows.ToList(), hops));
    }

    /// <summary>A message or dataframe arrived on one of the room's Links: the stacks receiving on it hear it, with the
    /// room it came from. One sent in this room is part of the sending execution, as a signal is; one from another
    /// room starts its own.</summary>
    public Task LinkReceivedAsync(RoomItemStore.PlacedItem link, WiredLinkPayload payload)
    {
        if (_boxes.Count == 0 || RunsSpent)
            return Task.CompletedTask;

        FlashItem(link);
        return FireAsync(new WiredEvent(
            payload.IsDataframe ? WiredEventType.LinkDataframeReceived : WiredEventType.LinkMessageReceived,
            Item: link,
            Text: payload.IsDataframe ? null : payload.Message)
        {
            LinkSourceRoomId = payload.SourceRoomId,
            LinkRows = payload.Rows,
            LinkHops = payload.Hops,
        });
    }

    public void SpeakAt(RoomItemStore.PlacedItem item, string text, int style) =>
        broadcast(ChatMessageComposer.OnTile(text, style, item.X, item.Y));
}
