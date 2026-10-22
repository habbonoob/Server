using Habbo.Domain.Users;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>One friend as the client's messenger reads them.</summary>
[WireStruct]
public sealed partial record FriendData(
    int Id,
    string Name,
    /// <summary>The character code of the gender letter, not a zero or a one: the client's own
    /// Friend class defines them as "M".charCodeAt(0) and "F".charCodeAt(0).</summary>
    int Gender,
    bool Online,
    bool FollowingAllowed,
    string Figure,
    int CategoryId,
    string Motto,
    string RealName,
    string FacebookId,
    bool PersistedMessageUser,
    bool VipMember,
    bool PocketHabboUser,
    [property: WireFormat(WireType.I16)] short RelationshipStatus)
{
    /// <summary>A console link the player has no Facebook for.</summary>
    private const string NoFacebookId = "";

    public static FriendData From(FriendView friend) => new(
        friend.AccountId,
        friend.Username,
        // The client reads the gender as the letter's character code, not as a number.
        friend.Gender.ToLetter()[0],
        friend.Online,
        friend.FollowingAllowed,
        friend.Figure,
        friend.CategoryId,
        friend.Motto,
        friend.RealName,
        NoFacebookId,
        PersistedMessageUser: friend.IsGroupChat,
        friend.IsVip,
        PocketHabboUser: false,
        (short)friend.Relationship);
}
