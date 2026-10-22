namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>InstantMessageError's codes, as the messenger's messenger.error.* texts word them.</summary>
public static class InstantMessageErrors
{
    public const int ReceiverMuted = 3;
    public const int SenderMuted = 4;
    public const int Offline = 5;
    public const int NotFriend = 6;
    public const int Busy = 7;
    public const int ReceiverHasNoChat = 8;
    public const int SenderHasNoChat = 9;
    public const int OfflineFailed = 10;
    public const int NotGroupMember = 11;
    public const int NotGroupAdmin = 12;
    public const int SenderUnavailable = 13;
    public const int RecipientUnavailable = 14;
}
