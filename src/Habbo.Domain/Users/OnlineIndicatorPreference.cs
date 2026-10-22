namespace Habbo.Domain.Users;

/// <summary>Who sees a player online, in the order of the client's settings menu.</summary>
public enum OnlineIndicatorPreference
{
    Everyone = 0,

    /// <summary>Only the friends the player has given a relationship status.</summary>
    RelationshipStatus = 1,

    Nobody = 2,
}
