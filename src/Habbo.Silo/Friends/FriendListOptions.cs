namespace Habbo.Silo.Friends;

/// <summary>What the hotel allows in a friend list (config section "FriendList").</summary>
public sealed class FriendListOptions
{
    /// <summary>How many friends an ordinary player may keep.</summary>
    public int NormalFriendLimit { get; set; } = 300;

    /// <summary>How many a club member may keep. The client shows both so a player can see what
    /// membership would buy them.</summary>
    public int ExtendedFriendLimit { get; set; } = 1100;
}
