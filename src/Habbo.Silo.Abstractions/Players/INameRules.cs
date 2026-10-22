namespace Habbo.Silo.Abstractions.Players;

/// <summary>The hotel's rules for account names, one set for registering and for changing a name in the hotel.</summary>
public interface INameRules
{
    /// <summary>Whether a name will do. A player keeping their own name in another case passes it as currentName.</summary>
    Task<NameVerdict> JudgeAsync(string name, string? currentName = null);
}
