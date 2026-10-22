using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Rooms;

/// <summary>How a high score board takes a game's results, by its score type, and forgets them, by its clear type — the
/// client's numbers for both (HighScoreDisplayWidget). A per team board keeps each team's best score, a most wins
/// board counts each team's wins, and a classic board keeps every score. The time boards count a game's time, which
/// the room's games do not keep, so nothing is written on them.</summary>
public static class HighScoreBoards
{
    public const int PerTeam = 0;
    public const int MostWins = 1;
    public const int Classic = 2;

    public const int AllTime = 0;
    public const int Daily = 1;
    public const int Weekly = 2;
    public const int Monthly = 3;

    /// <summary>Whether the board takes games' results at all.</summary>
    public static bool Records(FurniDataView board) => board.ScoreType is PerTeam or MostWins or Classic;

    /// <summary>The board with the game's results written on it, best first, and no more rows than it may hold.</summary>
    public static FurniDataView Record(FurniDataView board, IReadOnlyList<HighScoreResult> results, DateTimeOffset now, TimeZoneInfo zone, int maxEntries)
    {
        var entries = Current(board, now, zone);
        foreach (var result in results.Where(r => r.Users.Count > 0))
        {
            var index = entries.FindIndex(e => SameTeam(e.Users, result.Users));
            switch (board.ScoreType)
            {
                case PerTeam when index < 0:
                    entries.Add(new FurniHighScoreEntryView(result.Score, [.. result.Users], now));
                    break;
                case PerTeam when result.Score > entries[index].Score:
                    entries[index] = entries[index] with { Score = result.Score, RecordedAt = now };
                    break;
                case MostWins when result.Won && index < 0:
                    entries.Add(new FurniHighScoreEntryView(1, [.. result.Users], now));
                    break;
                case MostWins when result.Won:
                    entries[index] = entries[index] with { Score = entries[index].Score + 1, RecordedAt = now };
                    break;
                case Classic:
                    entries.Add(new FurniHighScoreEntryView(result.Score, [.. result.Users], now));
                    break;
            }
        }

        return board with
        {
            HighScoreEntries = entries.OrderByDescending(e => e.Score).ThenBy(e => e.RecordedAt).Take(maxEntries).ToList(),
        };
    }

    /// <summary>The rows written since the board last cleared.</summary>
    public static List<FurniHighScoreEntryView> Current(FurniDataView board, DateTimeOffset now, TimeZoneInfo zone)
    {
        var since = PeriodStart(board.ClearType ?? AllTime, now, zone);
        return (board.HighScoreEntries ?? []).Where(e => since is null || e.RecordedAt >= since).ToList();
    }

    /// <summary>When the board last cleared, in the room's time: midnight today, Monday, the first of the month; never
    /// for an all-time board.</summary>
    private static DateTimeOffset? PeriodStart(int clearType, DateTimeOffset now, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var today = new DateTimeOffset(local.Date, local.Offset);
        return clearType switch
        {
            Daily => today,
            Weekly => today.AddDays(-(((int)local.DayOfWeek + 6) % 7)),
            Monthly => today.AddDays(1 - local.Day),
            _ => null,
        };
    }

    private static bool SameTeam(IReadOnlyCollection<string> a, IReadOnlyCollection<string> b) =>
        a.Count == b.Count && a.All(name => b.Contains(name, StringComparer.OrdinalIgnoreCase));
}
