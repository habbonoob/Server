using Habbo.Domain.Games;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Games.SnowWar;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Habbo.Silo.Grains.Games;

/// <summary>The end of the game: the team with the most points wins (the same points: a tie), the results go to the
/// players and into their accounts, and after a while the game closes.</summary>
public sealed partial class SnowWarGameGrain
{
    /// <summary>How the game ended, as the results screen reads it: a team won, or it was a tie.</summary>
    private const int TeamWon = 1;
    private const int Tie = 2;

    /// <summary>"Nobody" for the best player of a kind.</summary>
    private const int Nobody = 0;

    /// <summary>The round is over: the arena goes at once, the results come after it.</summary>
    private const int ArenaEndsNow = 0;

    private async Task EndAsync()
    {
        _timer?.Dispose();
        _phase = SnowWarPhase.Results;

        var scores = _arena.TeamScores;
        var best = scores.Count == 0 ? 0 : scores.Max();
        var winners = Enumerable.Range(1, scores.Count).Where(team => scores[team - 1] == best).ToList();
        var result = winners.Count == 1
            ? new Game2GameResult(_arena.IsDeathMatch, TeamWon, winners[0])
            : new Game2GameResult(_arena.IsDeathMatch, Tie, 0);

        var teams = Enumerable.Range(1, scores.Count).Select(team => new Game2TeamScoreData(team, scores[team - 1],
            _players.Values.Where(p => p.Team == team && _humans.ContainsKey(p.Id)).Select(p =>
            {
                var human = _humans[p.Id];
                var stats = _stats.Of(human.Id);
                return new Game2TeamPlayerData(p.Name, p.AccountId, p.Figure, p.Gender, human.Score, new Game2PlayerStatsData(
                    human.Score, stats.Kills, stats.Deaths, stats.SnowballHits, stats.SnowballHitsTaken, stats.SnowballsThrown,
                    stats.SnowballsCreated, stats.SnowballsFromMachine, 0, 0));
            }).ToList())).ToList();

        await SendAsync(new Game2StageEndingMessageComposer(ArenaEndsNow));
        await SendAsync(new Game2GameEndingMessageComposer(Rules.ResultsSeconds, result, teams,
            new Game2SnowWarGameStats(BestAt(s => s.Kills), BestAt(s => s.SnowballHits))));

        // the players are free to join another game, or this one again, while the results are up; the directory hears
        // it first, so a failure keeping the results cannot keep them stuck in this game or the arena taken
        await Directory.GameOverAsync(_setup.GameId, [.. _players.Keys]);
        await Directory.GameEndedAsync(_setup.GameId);

        try
        {
            await RecordResultsAsync(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SnowStorm game {GameId}: the results could not be kept", _setup.GameId);
        }

        logger.LogInformation("SnowStorm game {GameId} is over: {Scores}", _setup.GameId, string.Join(" - ", scores));

        _timer = this.RegisterGrainTimer(_ =>
            {
                _phase = SnowWarPhase.Over;
                _timer?.Dispose();
                DeactivateOnIdle();
                return Task.CompletedTask;
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(Rules.ResultsSeconds), Period = Timeout.InfiniteTimeSpan });
    }

    /// <summary>Keeps the results of those who played to the end; who quit was kept as they left.</summary>
    private Task RecordResultsAsync(Game2GameResult result) =>
        accounts.RecordAsync(_players.Values.Where(p => _humans.ContainsKey(p.Id) && !_gone.Contains(p.Id)).Select(p =>
        {
            var human = _humans[p.Id];
            var stats = _stats.Of(human.Id);
            return new SnowWarGameResult
            {
                UserId = p.Id,
                GameId = _setup.GameId,
                FieldType = _level.FieldType,
                Team = p.Team,
                Won = Won(result, p),
                Score = human.Score,
                Kills = stats.Kills,
                Deaths = stats.Deaths,
                SnowballHits = stats.SnowballHits,
                SnowballHitsTaken = stats.SnowballHitsTaken,
                SnowballsThrown = stats.SnowballsThrown,
                SnowballsCreated = stats.SnowballsCreated,
                SnowballsFromMachine = stats.SnowballsFromMachine,
            };
        }).ToList());

    /// <summary>Whether the player won: their team did, or, in a game of one team, they alone scored the most.</summary>
    private bool Won(Game2GameResult result, SnowWarPlayer player)
    {
        if (!_arena.IsDeathMatch)
            return result.ResultType == TeamWon && result.WinnerId == player.Team;

        var scores = _players.Values.Where(p => _humans.ContainsKey(p.Id) && !_gone.Contains(p.Id))
            .Select(p => (p.Id, _humans[p.Id].Score)).ToList();
        var best = scores.Max(s => s.Score);
        return best > 0 && scores.Count(s => s.Score == best) == 1 && scores.Single(s => s.Score == best).Id == player.Id;
    }

    /// <summary>Keeps a game for a player who quit it: a game played, so it counts towards the day's free ones,
    /// but lost and scoring nothing, so it gives no points, wins or achievements.</summary>
    private async Task RecordQuitAsync(SnowWarPlayer player)
    {
        try
        {
            await accounts.RecordAsync([new SnowWarGameResult
            {
                UserId = player.Id,
                GameId = _setup.GameId,
                FieldType = _level.FieldType,
                Team = player.Team,
            }]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SnowStorm game {GameId}: the quit of {Player} could not be kept", _setup.GameId, player.Name);
        }
    }

    /// <summary>The account id of the player who did best at something, if anyone did it at all.</summary>
    private int BestAt(Func<SnowWarPlayerStats, int> measure)
    {
        var best = _players.Values.Where(p => _humans.ContainsKey(p.Id) && !_gone.Contains(p.Id))
            .Select(p => (Player: p, Value: measure(_stats.Of(_humans[p.Id].Id))))
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();
        return best.Player is not null && best.Value > 0 ? best.Player.AccountId : Nobody;
    }
}
