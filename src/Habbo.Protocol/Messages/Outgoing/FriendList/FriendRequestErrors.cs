namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>The friend request errors the friend list alerts with (MessengerError, and each
/// failure in AcceptFriendResult), as friendlist.error.* words them.</summary>
public static class FriendRequestErrors
{
    public const int OwnListFull = 1;
    public const int RequesterListFull = 2;
    public const int RequestsDisabled = 3;
    public const int NotFound = 4;
    public const int BlockedByThem = 7;
    public const int BlockedByYou = 8;
}
