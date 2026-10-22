namespace Habbo.Silo.Competitions;

/// <summary>A room competition the hotel runs: while it is on, owners enter a room made for it (after reading the rules)
/// and others vote for the rooms entered. Its name and texts are roomcompetition.&lt;code&gt;.* in the gamedata.</summary>
public sealed class RoomCompetition
{
    /// <summary>The hotel's number for it, and its code.</summary>
    public int GoalId { get; set; }
    public string GoalCode { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>The furni (by class name) a room must have to be entered.</summary>
    public List<string> RequiredFurni { get; set; } = [];

    /// <summary>Whether owners must accept the rules before entering a room.</summary>
    public bool RulesToAccept { get; set; } = true;

    /// <summary>How many votes each player has a day.</summary>
    public int VotesPerDay { get; set; } = 3;

    /// <summary>The badge a player must hold to vote (Respected Habbo IV is ACH_RespectEarned4); none for anyone.</summary>
    public string VoteRequiredBadge { get; set; } = string.Empty;

    public bool IsOn(DateTimeOffset now) => now >= StartsAt && now < EndsAt;
}
