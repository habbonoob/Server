namespace Habbo.Silo.Abstractions.Players;

/// <summary>Whether a name will do for an account.</summary>
public enum NameVerdict
{
    Available,
    TooShort,
    TooLong,

    /// <summary>A character names may not hold, or a filtered word.</summary>
    Invalid,
    Taken,
}
