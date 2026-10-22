using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>1012 WhisperMessageComposer — the same layout, shown only to the target, and written for each of the
/// two as chat is (<see cref="ChatMessageComposer"/>).</summary>
[Message(ServerMessages.WhisperMessageComposer)]
public sealed partial record WhisperMessageComposer(
    int RoomIndex,
    string Text,
    int Gesture,
    int StyleId,
    IReadOnlyList<ChatLinkWireData> Links,
    int TrackingId,
    int ReceiverRoomIndex,
    int ChatBubbleWidthOverride)
{
    public static WhisperMessageComposer From(ChatMessage message, int receiverRoomIndex, bool toSpeaker) => new(
        message.RoomIndex, message.Text, (int)message.Gesture, message.StyleId, ChatLinks.From(message.Links),
        toSpeaker ? message.TrackingId : ChatMessageComposer.Untracked, receiverRoomIndex, message.BubbleWidth);
}
