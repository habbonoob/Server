namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>The game's clock and its queue of events: each turn has three subturns, and each subturn first applies
/// the events queued for it, then moves everything once. At the end of a turn its checksum is taken. The client runs
/// the very same steps on the same events, and the checksums tell whether it kept up. A port of the client's
/// SynchronizedGameArena.</summary>
internal sealed class SnowWarArena
{
    public const int SubturnsPerTurn = 3;

    /// <summary>How long one subturn lasts on the client.</summary>
    public static readonly TimeSpan SubturnLength = TimeSpan.FromMilliseconds(50);

    private readonly Dictionary<int, List<SnowWarEvent>[]> _events = [];
    private readonly int[] _teamScores;

    public SnowWarArena(int numberOfTeams)
    {
        NumberOfTeams = numberOfTeams;
        _teamScores = new int[numberOfTeams];
    }

    public SnowWarStage Stage { get; set; } = null!;
    public int NumberOfTeams { get; }
    public int Turn { get; private set; }

    public bool IsDeathMatch => NumberOfTeams == 1;

    public IReadOnlyList<int> TeamScores => _teamScores;

    public void AddTeamScore(int team, int score)
    {
        if (team > 0 && team <= NumberOfTeams)
            _teamScores[team - 1] += score;
    }

    /// <summary>Queues an event for a subturn of a turn to come.</summary>
    public void AddEvent(int turn, int subturn, SnowWarEvent gameEvent) => QueueOf(turn)[subturn].Add(gameEvent);

    /// <summary>The events queued for a turn, subturn by subturn.</summary>
    public IReadOnlyList<IReadOnlyList<SnowWarEvent>> EventsOf(int turn) => QueueOf(turn);

    /// <summary>Plays the current turn through; its checksum.</summary>
    public int PlayTurn()
    {
        var queue = QueueOf(Turn);
        for (var subturn = 0; subturn < SubturnsPerTurn; subturn++)
        {
            foreach (var gameEvent in queue[subturn])
                gameEvent.Apply(Stage);
            Stage.Subturn();
        }

        var checksum = Stage.Checksum(Turn);
        _events.Remove(Turn);
        Turn++;
        return checksum;
    }

    private List<SnowWarEvent>[] QueueOf(int turn)
    {
        if (!_events.TryGetValue(turn, out var queue))
        {
            queue = new List<SnowWarEvent>[SubturnsPerTurn];
            for (var i = 0; i < SubturnsPerTurn; i++)
                queue[i] = [];
            _events[turn] = queue;
        }

        return queue;
    }
}
