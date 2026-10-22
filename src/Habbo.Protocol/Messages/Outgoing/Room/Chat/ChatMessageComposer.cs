using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>947 ChatMessageComposer — normal talk. Written for each listener, as Habbo does: the speaker's copy
/// carries the tracking id their client sent, everyone else's none; each names the room index of whoever it is for;
/// and no bubble width is forced.</summary>
[Message(ServerMessages.ChatMessageComposer)]
public sealed partial record ChatMessageComposer(
    int RoomIndex,
    string Text,
    int Gesture,
    int StyleId,
    IReadOnlyList<ChatLinkWireData> Links,
    int TrackingId,
    int ReceiverRoomIndex,
    int ChatBubbleWidthOverride)
{
    /// <summary>The tracking id of a copy for anyone but the speaker.</summary>
    public const int Untracked = -1;

    /// <summary>The receiver of a copy with no avatar in the room: a spectator.</summary>
    public const int NoReceiver = -1;

    /// <summary>The bubble width the room's chat settings give, not one forced.</summary>
    public const int RoomBubbleWidth = -1;

    public static ChatMessageComposer From(ChatMessage message, int receiverRoomIndex, bool toSpeaker) => new(
        message.RoomIndex, message.Text, (int)message.Gesture, message.StyleId,
        ChatLinks.From(message.Links), toSpeaker ? message.TrackingId : Untracked, receiverRoomIndex, message.BubbleWidth);
}
