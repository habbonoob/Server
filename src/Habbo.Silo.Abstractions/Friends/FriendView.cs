using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

/// <summary>One friend. The gender is the letter rather than a number, because the wire wants
/// its character code and only the gateway should know that.</summary>
[GenerateSerializer]
public partial record FriendView(
    [property: Id(0)] int AccountId,
    [property: Id(1)] string Username,
    [property: Id(2)] Gender Gender,
    [property: Id(3)] bool Online,
    [property: Id(4)] string Figure,
    [property: Id(5)] int CategoryId,
    [property: Id(6)] string Motto,
    [property: Id(7)] string RealName,
    [property: Id(8)] RelationshipStatus Relationship,
    [property: Id(9)] bool IsVip,
    /// <summary>Whether the player may follow them into the room they are in.</summary>
    [property: Id(10)] bool FollowingAllowed = false,
    /// <summary>A group's chat rather than a person: its id is the group's, negated, and its
    /// figure the group's badge.</summary>
    [property: Id(11)] bool IsGroupChat = false);
