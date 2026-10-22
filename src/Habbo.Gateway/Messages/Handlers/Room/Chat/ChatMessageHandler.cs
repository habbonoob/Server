using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Chat;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Gateway.Messages.Handlers.Room.Chat;

/// <summary>1119 Chat / 1400 Shout / 802 Whisper — the room grain decides who hears it.
/// The three share a payload, so they share a handler; a whisper's text names who it is for first.</summary>
internal sealed class ChatMessageHandler :
    IMessageHandler<ChatMessageEvent>,
    IMessageHandler<ShoutMessageEvent>,
    IMessageHandler<WhisperMessageEvent>
{
    /// <summary>The client's own "no tracking id" value, used when it sends none.</summary>
    private const int NoTrackingId = -1;

    public Task<bool> HandleAsync(GatewaySessionContext context, ChatMessageEvent message, CancellationToken ct) =>
        SayAsync(context, message.Text, message.StyleId, message.TrackingId, RoomChatKind.Talk);

    public Task<bool> HandleAsync(GatewaySessionContext context, ShoutMessageEvent message, CancellationToken ct) =>
        SayAsync(context, message.Text, message.StyleId, message.TrackingId, RoomChatKind.Shout);

    public Task<bool> HandleAsync(GatewaySessionContext context, WhisperMessageEvent message, CancellationToken ct) =>
        SayAsync(context, message.Text, message.StyleId, message.TrackingId, RoomChatKind.Whisper);

    private static async Task<bool> SayAsync(
        GatewaySessionContext context, string text, int styleId, int? trackingId, RoomChatKind kind)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.ChatAsync(player.Id, text, styleId, trackingId ?? NoTrackingId, kind);
        return true;
    }
}
