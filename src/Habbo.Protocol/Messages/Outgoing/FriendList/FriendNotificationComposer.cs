using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>FriendNotificationComposer — something a friend did, shown on the friend bar</summary>
[Message(ServerMessages.FriendNotificationComposer)]
public sealed partial record FriendNotificationComposer(string AvatarId, int TypeCode, string Message)
{
    /// <summary>The friend started a room event; the message is its name.</summary>
    public const int RoomEvent = 0;

    /// <summary>The friend reached an achievement level; the message is its badge.</summary>
    public const int Achievement = 1;
}
