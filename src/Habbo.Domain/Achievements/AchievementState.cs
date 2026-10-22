namespace Habbo.Domain.Achievements;

/// <summary>Where the client files an achievement. It is sent as a short and the client only
/// ever tests it against <see cref="Archived"/>, so the other values differ in name alone —
/// they are kept apart because the hotel still needs to tell a retired one from a live one.
/// </summary>
public enum AchievementState : short
{
    /// <summary>Drawn under its own category, which is what a live achievement wants.</summary>
    Active = 1,

    /// <summary>Filed under the client's archive tab instead of its category.</summary>
    Archived = 2,

    /// <summary>Retired. The client draws it like an active one, so an achievement meant to be
    /// out of sight is left without a category, which drops it on arrival.</summary>
    Retired = 3,
}
