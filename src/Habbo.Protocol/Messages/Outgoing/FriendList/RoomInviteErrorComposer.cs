using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>RoomInviteErrorComposer — an invitation that did not reach everyone; with
/// <see cref="NotDelivered"/> it names who missed it.</summary>
[Message(ServerMessages.RoomInviteErrorComposer)]
public sealed partial record RoomInviteErrorComposer(
    int ErrorCode,
    [property: WireWhen("NamesRecipients")] IReadOnlyList<int>? FailedRecipients)
{
    /// <summary>Some of the friends were not there to be invited.</summary>
    public const int NotDelivered = 1;

    public bool NamesRecipients => ErrorCode == NotDelivered;
}
