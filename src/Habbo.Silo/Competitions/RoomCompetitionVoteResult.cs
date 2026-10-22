namespace Habbo.Silo.Competitions;

/// <summary>Whether a player may vote, as roomcompetition.caption.vote.&lt;value&gt; explains it.</summary>
public enum RoomCompetitionVoteResult
{
    MayVote = 0,

    /// <summary>The talent track steps it asks for are not unlocked.</summary>
    TalentTrackLocked = 1,

    /// <summary>The badge it asks for is not held.</summary>
    BadgeMissing = 2,
}
