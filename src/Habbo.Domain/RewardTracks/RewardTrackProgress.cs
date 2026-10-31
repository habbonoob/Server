using Habbo.Domain.Users;

namespace Habbo.Domain.RewardTracks;

/// <summary>A player's progress on one reward track: the points they have earned, whether they bought its premium
/// side, how far each task has counted and which prizes they have claimed. The tracks themselves are seed data
/// (seed/reward_tracks.json), named here by id.</summary>
public sealed class RewardTrackProgress
{
    public const int MaxIdLength = 64;

    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string TrackId { get; set; } = string.Empty;

    public int Points { get; set; }
    public bool Premium { get; set; }

    /// <summary>Each task's count, by task id; a task not in it has counted nothing.</summary>
    public Dictionary<string, int> Tasks { get; set; } = [];

    /// <summary>The ids of the prizes claimed.</summary>
    public List<string> Claimed { get; set; } = [];
}
