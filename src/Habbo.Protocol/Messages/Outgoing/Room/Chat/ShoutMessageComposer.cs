using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>935 ShoutMessageComposer — the same layout as chat, drawn louder, and written for each listener as
/// chat is (<see cref="ChatMessageComposer"/>).</summary>
[Message(ServerMessages.ShoutMessageComposer)]
public sealed partial record ShoutMessageComposer(
    int RoomIndex,
    string Text,
    int Gesture,
    int StyleId,
    IReadOnlyList<ChatLinkWireData> Links,
    int TrackingId,
    int ReceiverRoomIndex,
    int ChatBubbleWidthOverride)
{
    public static ShoutMessageComposer From(ChatMessage message, int receiverRoomIndex, bool toSpeaker) => new(
        message.RoomIndex, message.Text, (int)message.Gesture, message.StyleId, ChatLinks.From(message.Links),
        toSpeaker ? message.TrackingId : ChatMessageComposer.Untracked, receiverRoomIndex, message.BubbleWidth);
}
