namespace Habbo.Domain.Friends;

/// <summary>What a player has marked a friend as, shown beside them in the friend list. The
/// client's own RelationshipStatusEnum names the first as "heart" in its string table while
/// leaving the constant unnamed; the numbers are what travel.</summary>
public enum RelationshipStatus : short
{
    None = 0,
    Heart = 1,
    Smile = 2,
    Bobba = 3,
}
