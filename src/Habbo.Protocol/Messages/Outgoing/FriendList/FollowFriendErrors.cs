namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>FollowFriendFailed's reasons, as friendlist.followerror.* words them.</summary>
public static class FollowFriendErrors
{
    public const int NotFriend = 0;
    public const int Offline = 1;
    public const int HotelView = 2;
    public const int Prevented = 3;
}
