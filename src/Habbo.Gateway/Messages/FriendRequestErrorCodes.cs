using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages;

/// <summary>The friend list's error code for why a request or an acceptance failed.</summary>
internal static class FriendRequestErrorCodes
{
    /// <summary>A failure the client has no words for (asking twice, asking a friend, oneself).</summary>
    public const int Unexplained = 0;

    public static int Of(FriendRequestFailure failure) => failure switch
    {
        FriendRequestFailure.UnknownUser => FriendRequestErrors.NotFound,
        FriendRequestFailure.Blocked => FriendRequestErrors.BlockedByThem,
        FriendRequestFailure.BlockedByRequester => FriendRequestErrors.BlockedByYou,
        FriendRequestFailure.OwnListFull => FriendRequestErrors.OwnListFull,
        FriendRequestFailure.TargetListFull => FriendRequestErrors.RequesterListFull,
        FriendRequestFailure.RequestsDisabled => FriendRequestErrors.RequestsDisabled,
        _ => Unexplained,
    };
}
