using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>947 ChatMessageComposer — normal talk. Written for each listener, as Habbo does: the speaker's copy
/// carries the tracking id their client sent, everyone else's none; each names the room index of whoever it is for;
/// and no bubble width is forced. From October 2026 a bubble may stand on a tile instead, said by no one and shown
/// without a name (the client reads the name flag and the tile only when they are there).</summary>
[Message(ServerMessages.ChatMessageComposer)]
public sealed partial record ChatMessageComposer(
    int RoomIndex,
    string Text,
    int Gesture,
    int StyleId,
    IReadOnlyList<ChatLinkWireData> Links,
    int TrackingId,
    int ReceiverRoomIndex,
    int ChatBubbleWidthOverride,
    [property: WireSince(ClientReleases.October2026), WireWhen("IsLocated")] bool? HideUsername = null,
    [property: WireSince(ClientReleases.October2026), WireWhen("IsLocated")] int? LocationX = null,
    [property: WireSince(ClientReleases.October2026), WireWhen("IsLocated")] int? LocationY = null)
{
    /// <summary>The tracking id of a copy for anyone but the speaker.</summary>
    public const int Untracked = -1;

    /// <summary>The receiver of a copy with no avatar in the room: a spectator.</summary>
    public const int NoReceiver = -1;

    /// <summary>The bubble width the room's chat settings give, not one forced.</summary>
    public const int RoomBubbleWidth = -1;

    /// <summary>The room index of a bubble no one in the room says.</summary>
    public const int NoSpeaker = -1;

    /// <summary>Whether the bubble stands on a tile rather than over its speaker.</summary>
    public bool IsLocated => LocationX is not null;

    public static ChatMessageComposer From(ChatMessage message, int receiverRoomIndex, bool toSpeaker) => new(
        message.RoomIndex, message.Text, (int)message.Gesture, message.StyleId,
        ChatLinks.From(message.Links), toSpeaker ? message.TrackingId : Untracked, receiverRoomIndex, message.BubbleWidth);

    /// <summary>A bubble on a tile, said by no one, in a style.</summary>
    public static ChatMessageComposer OnTile(string text, int styleId, int x, int y) => new(
        NoSpeaker, text, 0, styleId, [], Untracked, NoReceiver, RoomBubbleWidth, HideUsername: true, LocationX: x, LocationY: y);
}
