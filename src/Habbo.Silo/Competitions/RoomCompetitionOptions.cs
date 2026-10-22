namespace Habbo.Silo.Competitions;

/// <summary>The room competitions the hotel runs; none by default.</summary>
public sealed class RoomCompetitionOptions
{
    public List<RoomCompetition> Competitions { get; set; } = [];
}
