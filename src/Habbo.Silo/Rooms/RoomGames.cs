using System.Globalization;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Rooms;

/// <summary>The games of one room: its clocks (countdown game timers and counters that count up),
/// the game they run, and its four teams with their scores. Starting a clock that was reset starts
/// the game, which clears the scores; a countdown running out, or a clock being stopped, ends it.
/// Scoreboards show their team's score and gates how many are in their team, as their furnidata
/// custom params say which team they are.
///
/// Users join a team through a gate or a wired effect, wearing the team's effect: the gate's own
/// (from its custom params), or the effect of the team type the wired chose (Wired, Banzai,
/// Freeze), found in Habbo's effect map by name. Teams are the room's four colours; the type only
/// decides the look, so every scoreboard of a colour shows that colour's score.
///
/// A game is every game in the room at once (Habbo: starting a Freeze counter lit the Banzai tiles too): its
/// Banzai tiles and puck (<see cref="BanzaiArena"/>) and its Freeze arena (<see cref="FreezeArena"/>). Only a game
/// two teams or more started in counts towards the game achievements; a one-team game in one's own room gave none
/// on Habbo but the Freeze power-ups.</summary>
public sealed class RoomGames(
    RoomItemStore items,
    RoomEntityStore entities,
    IFurniRoom furniture,
    IAvatarEffectCatalog effects,
    Action<RoomEntityStore.RoomEntity, int> applyEffect,
    GameRoomActions? actions = null)
{
    public const int NoTeam = 0;
    public const int FirstTeam = 1;
    public const int LastTeam = 4;

    // the teams, as the client numbers them
    public const int Red = 1;
    public const int Green = 2;
    public const int Blue = 3;
    public const int Yellow = 4;

    /// <summary>The effect libraries each team type dresses its teams in, by team (red, green, blue,
    /// yellow), as they are named in Habbo's effect map.</summary>
    private static readonly Dictionary<TeamType, string[]> TeamEffects = new()
    {
        [TeamType.Wired] = ["WiredTeamRed", "WiredTeamGreen", "WiredTeamBlue", "WiredTeamYellow"],
        [TeamType.Banzai] = ["BBRed", "BBgreen", "BBblue", "BByellow"],
        [TeamType.Freeze] = ["ESred", "ESgreen", "ESblue", "ESyellow"],
    };

    private readonly int[] _scores = new int[LastTeam + 1];
    private readonly Dictionary<int, GameClock> _clocks = [];
    private BanzaiArena? _banzai;
    private FreezeArena? _freeze;
    private FootballPitch? _football;

    private BanzaiArena Banzai => _banzai ??= new BanzaiArena(this, items, furniture, actions);
    private FreezeArena Freeze => _freeze ??= new FreezeArena(this, items, entities, furniture, actions, applyEffect);
    private FootballPitch Football => _football ??= new FootballPitch(items, entities, furniture, actions);

    /// <summary>The time zone the room tells time in, which its high score boards clear by.</summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>The most rows a high score board holds.</summary>
    public int HighScoreMaxEntries { get; set; }

    /// <summary>Whether a game is on.</summary>
    public bool Running { get; private set; }

    /// <summary>Counts the games started, so what is limited "per game" knows when a new one began.</summary>
    public int GameNumber { get; private set; }

    /// <summary>Whether the game on counts towards the game achievements: two teams or more were in it at its start.</summary>
    public bool CountsForAchievements { get; private set; }

    /// <summary>Told of every start, end, score change and clock tick, to pass on to the room's wired.</summary>
    public Func<GameEvent, Task>? OnEvent { get; set; }

    /// <summary>Told when a user joins a team or leaves one: they are playing, or no longer.</summary>
    public Action<RoomEntityStore.RoomEntity, bool>? OnPlayingChanged { get; set; }

    public int Score(int team) => team is >= FirstTeam and <= LastTeam ? _scores[team] : 0;

    public IEnumerable<RoomEntityStore.RoomEntity> Members(int team) => entities.Entities.Where(e => e.Team == team);

    /// <summary>The teams by score, best first: the placing team-is-winning asks about. Only teams
    /// with players, or with points, take part.</summary>
    public List<int> Ranking() =>
        Enumerable.Range(FirstTeam, LastTeam)
            .Where(team => _scores[team] != 0 || Members(team).Any())
            .OrderByDescending(team => _scores[team])
            .ToList();

    // ---------- clocks ----------

    /// <summary>Whether the item is one of the room's clocks.</summary>
    public static bool IsClock(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.InteractionName == GameClockInteraction.Name;

    public GameClock? Clock(RoomItemStore.PlacedItem item)
    {
        if (!IsClock(item))
            return null;

        if (_clocks.TryGetValue(item.Item.Id, out var clock) && clock.Item == item)
            return clock;

        clock = GameClock.For(item);
        _clocks[item.Item.Id] = clock;
        return clock;
    }

    /// <summary>A click on a clock: 1 starts or stops it, 2 resets it (a countdown to its next preset
    /// time) while it is stopped.</summary>
    public async Task UseClockAsync(RoomItemStore.PlacedItem item, int param)
    {
        if (Clock(item) is not { } clock)
            return;

        switch (param)
        {
            case GameClockInteraction.StartStop when clock.Running:
                clock.Running = false;
                await ShowAsync(clock);
                break;
            case GameClockInteraction.StartStop:
                // a countdown that ran out starts over from its time (Habbo: a Banzai counter at 0 started from 30)
                if (clock.CountsDown && clock.Pulses <= 0)
                    clock.Restart();
                await StartAsync(clock);
                break;
            case GameClockInteraction.Reset when !clock.Running:
                clock.NextPreset();
                await ShowAsync(clock);
                break;
        }
    }

    public async Task StartAsync(GameClock clock)
    {
        if (clock.Running || (clock.CountsDown && clock.Pulses <= 0))
            return;

        if (clock.Fresh && !Running)
            await StartGameAsync(clock);

        clock.Running = true;
        clock.Fresh = false;
        await ShowAsync(clock);
    }

    public async Task StopAsync(GameClock clock)
    {
        clock.Running = false;
        await ShowAsync(clock);
        if (Running)
            await EndGameAsync();
    }

    public async Task PauseAsync(GameClock clock)
    {
        clock.Running = false;
        await ShowAsync(clock);
    }

    public async Task ResumeAsync(GameClock clock)
    {
        if (clock.Fresh || (clock.CountsDown && clock.Pulses <= 0))
            return;

        clock.Running = true;
        await ShowAsync(clock);
    }

    public async Task ResetAsync(GameClock clock)
    {
        clock.Running = false;
        clock.Restart();
        await ShowAsync(clock);
    }

    /// <summary>Sets a clock's time, in pulses of half a second.</summary>
    public async Task SetTimeAsync(GameClock clock, int pulses)
    {
        clock.Pulses = Math.Max(0, pulses);
        await ShowAsync(clock);
    }

    /// <summary>The room's pulse: the Banzai tiles stepped on show, running clocks count, a countdown that runs
    /// out ends the game.</summary>
    public async Task PulseAsync()
    {
        await Banzai.PulseAsync();
        foreach (var clock in _clocks.Values.Where(c => c.Running).ToList())
        {
            if (items.Find(clock.Item.Item.Id) != clock.Item)
            {
                _clocks.Remove(clock.Item.Item.Id);
                continue;
            }

            // the time shown each second is kept in the room only; it is saved when the clock stops or is set
            clock.Pulses += clock.CountsDown ? -1 : 1;
            var runsOut = clock.CountsDown && clock.Pulses <= 0;
            if (clock.Pulses % 2 == 0 || runsOut)
                await ShowAsync(clock, persist: runsOut);

            await RaiseAsync(new GameEvent(GameEventType.ClockTicked, Clock: clock));

            if (clock.CountsDown && clock.Pulses <= 0)
            {
                clock.Running = false;
                if (Running)
                    await EndGameAsync();
            }
        }
    }

    private Task ShowAsync(GameClock clock, bool persist = true) =>
        furniture.UpdateAsync(clock.Item, clock.Item.Data with { State = (clock.Pulses / 2).ToString(CultureInfo.InvariantCulture) }, persist);

    // ---------- the game ----------

    /// <summary>A game starts, by a clock: every arena in the room is set for it, but only a Freeze counter makes it a
    /// game of Freeze (lives, the starting points, snowballs), so a Banzai game beside a Freeze arena is not scored on
    /// Freeze's head start.</summary>
    private async Task StartGameAsync(GameClock clock)
    {
        Running = true;
        GameNumber++;
        CountsForAchievements = Players().Select(e => e.Team).Distinct().Count() >= 2;
        for (var team = FirstTeam; team <= LastTeam; team++)
            _scores[team] = 0;

        await ShowScoresAsync();
        await Banzai.StartAsync();
        await Freeze.StartAsync(FreezeArena.IsCounter(clock.Item));
        await RaiseAsync(new GameEvent(GameEventType.Started));
    }

    private async Task EndGameAsync()
    {
        Running = false;
        var winners = Winners();
        CountAchievements(winners);
        await Banzai.EndAsync(winners);
        await Freeze.EndAsync();
        await RecordHighScoresAsync();
        await RaiseAsync(new GameEvent(GameEventType.Ended));
    }

    /// <summary>Ends the game on now, as if its clocks ran out: Freeze, when one team is left.</summary>
    public async Task FinishAsync()
    {
        foreach (var clock in _clocks.Values.Where(c => c.Running).ToList())
        {
            // run out, so the next start begins a new game from the clock's time rather than resuming this one
            if (clock.CountsDown)
                clock.Pulses = 0;
            await StopAsync(clock);
        }
        if (Running)
            await EndGameAsync();
    }

    /// <summary>The users in teams: avatars, not bots or pets.</summary>
    private IEnumerable<RoomEntityStore.RoomEntity> Players() =>
        entities.Entities.Where(e => e.Team != NoTeam && e.Bot is null && e.Pet is null);

    /// <summary>The teams with the best score, if anyone scored.</summary>
    private List<int> Winners()
    {
        var best = Enumerable.Range(FirstTeam, LastTeam).Max(team => _scores[team]);
        return best > 0 ? Enumerable.Range(FirstTeam, LastTeam).Where(team => _scores[team] == best).ToList() : [];
    }

    /// <summary>Played and won, for each kind of game the room has, when the game counted.</summary>
    private void CountAchievements(IReadOnlyCollection<int> winners)
    {
        if (!CountsForAchievements || actions is null)
            return;

        var kinds = new List<(string Player, string Winner)>();
        if (items.Items.Any(BanzaiArena.IsTile))
            kinds.Add((AchievementNames.BattleBallPlayer, AchievementNames.BattleBallWinner));
        if (items.Items.Any(FreezeArena.IsTile))
            kinds.Add((AchievementNames.FreezePlayer, AchievementNames.FreezeWinner));

        foreach (var user in Players().ToList())
        {
            foreach (var (player, winner) in kinds)
            {
                actions.ProgressAchievement(user, player, 1);
                if (winners.Contains(user.Team))
                    actions.ProgressAchievement(user, winner, 1);
            }
        }
    }

    /// <summary>A double-click on a Freeze tile or block: a snowball, if the user may throw.</summary>
    public static bool IsThrow(RoomItemStore.PlacedItem item) => FreezeArena.Throws(item);

    public Task ThrowAsync(RoomEntityStore.RoomEntity user) => Freeze.ThrowAsync(user);

    /// <summary>Writes how each team that played did on the room's high score boards; the team with the best score
    /// won, and so did any that tied with it.</summary>
    private async Task RecordHighScoresAsync()
    {
        var boards = items.Items.Where(item => item.Interaction is HighScoreInteraction).ToList();
        var teams = Enumerable.Range(FirstTeam, LastTeam)
            .Select(team => (Team: team, Users: Members(team).Where(e => e.Bot is null && e.Pet is null).Select(e => e.Player.Username).ToList()))
            .Where(team => team.Users.Count > 0)
            .ToList();
        if (boards.Count == 0 || teams.Count == 0)
            return;

        var best = teams.Max(team => _scores[team.Team]);
        var results = teams.Select(team => new HighScoreResult(_scores[team.Team], team.Users, _scores[team.Team] == best)).ToList();
        var now = DateTimeOffset.UtcNow;
        await furniture.BatchedAsync(async () =>
        {
            foreach (var board in boards)
            {
                var data = HighScoreInteraction.BoardOf(board);
                if (HighScoreBoards.Records(data))
                    await furniture.UpdateAsync(board, HighScoreBoards.Record(data, results, now, Zone(), HighScoreMaxEntries));
            }
        });
    }

    /// <summary>Clears the boards whose day, week or month has passed since they were last written.</summary>
    public async Task ClearStaleHighScoresAsync()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var board in items.Items.Where(item => item.Interaction is HighScoreInteraction && item.Data.HighScoreEntries is { Count: > 0 }).ToList())
        {
            var current = HighScoreBoards.Current(board.Data, now, Zone());
            if (current.Count != board.Data.HighScoreEntries!.Count)
                await furniture.UpdateAsync(board, board.Data with { HighScoreEntries = current });
        }
    }

    private TimeZoneInfo Zone() =>
        Timezone.Length > 0 && TimeZoneInfo.TryFindSystemTimeZoneById(Timezone, out var zone) ? zone : TimeZoneInfo.Utc;

    // ---------- teams ----------

    /// <summary>Puts the user in a team, wearing the given effect; leaving the team it was in.</summary>
    public async Task JoinAsync(RoomEntityStore.RoomEntity user, int team, int effectId)
    {
        if (team is < FirstTeam or > LastTeam)
            return;

        var wasPlaying = user.Team != NoTeam;
        user.Team = team;
        applyEffect(user, effectId);
        if (!wasPlaying)
            OnPlayingChanged?.Invoke(user, true);
        await ShowGatesAsync();
    }

    /// <summary>Puts the user in a team of a team type, wearing that type's effect for it.</summary>
    public async Task JoinAsync(RoomEntityStore.RoomEntity user, int team, TeamType type)
    {
        if (team is < FirstTeam or > LastTeam || !TeamEffects.TryGetValue(type, out var libraries))
            return;

        var effectId = await effects.FindAsync(libraries[team - FirstTeam]) ?? 0;
        await JoinAsync(user, team, effectId);
    }

    public async Task LeaveAsync(RoomEntityStore.RoomEntity user)
    {
        if (user.Team == NoTeam)
            return;

        user.Team = NoTeam;
        applyEffect(user, 0);
        OnPlayingChanged?.Invoke(user, false);
        await ShowGatesAsync();
    }

    /// <summary>A user stepped onto an item, from a tile: a gate joins its team, or, for one already in it, leaves;
    /// the games' tiles, puck, power-ups and footballs answer for themselves; <paramref name="arriving"/> when the walk
    /// ends there.</summary>
    public async Task UserSteppedOnAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item, int fromX, int fromY, bool arriving = false)
    {
        Banzai.SteppedOn(user, item, fromX, fromY);
        await Football.SteppedOnAsync(user, item, fromX, fromY, arriving);
        await Freeze.SteppedOnAsync(user, item);
        if (item.Item.Definition!.InteractionName != GameGateInteraction.Name || GateOf(item) is not { } gate)
            return;

        if (user.Team == gate.Team)
            await LeaveAsync(user);
        else
            await JoinAsync(user, gate.Team, gate.EffectId);
    }

    /// <summary>A user walked off a tile: a football they stood on is heeled back.</summary>
    public void UserSteppedOff(RoomEntityStore.RoomEntity user, int fromX, int fromY) => Football.SteppedOff(user, fromX, fromY);

    /// <summary>A gate's team and effect, from its custom params: "team,effect".</summary>
    private static (int Team, int EffectId)? GateOf(RoomItemStore.PlacedItem item)
    {
        var parts = item.Item.Definition!.CustomParams.Split(',');
        return parts.Length == 2
               && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var team)
               && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var effect)
            ? (team, effect)
            : null;
    }

    private async Task ShowGatesAsync()
    {
        foreach (var gate in items.Items.Where(item => item.Item.Definition!.InteractionName == GameGateInteraction.Name).ToList())
        {
            if (GateOf(gate) is not { } of)
                continue;

            var top = WiredStates(gate).DefaultIfEmpty(0).Max();
            var count = Math.Min(Members(of.Team).Count(), top).ToString(CultureInfo.InvariantCulture);
            if (gate.Data.StateText != count)
                await furniture.UpdateAsync(gate, gate.Data with { State = count });
        }
    }

    private static IEnumerable<int> WiredStates(RoomItemStore.PlacedItem item) =>
        item.Item.Definition!.States.Select(s => s.State);

    // ---------- scores ----------

    /// <summary>Changes a team's score and shows it; wired hears the change.</summary>
    public async Task AddScoreAsync(int team, int points)
    {
        if (team is < FirstTeam or > LastTeam || points == 0)
            return;

        var old = _scores[team];
        // added as longs, so a score at its top stays there rather than wrapping round to nothing
        _scores[team] = (int)Math.Clamp((long)old + points, 0, int.MaxValue);
        await ShowScoresAsync();
        await RaiseAsync(new GameEvent(GameEventType.ScoreChanged, team, old, _scores[team]));
    }

    private async Task ShowScoresAsync()
    {
        foreach (var board in items.Items.Where(item => item.Item.Definition!.InteractionName == GameScoreboardInteraction.Name).ToList())
        {
            if (!int.TryParse(board.Item.Definition!.CustomParams, NumberStyles.Integer, CultureInfo.InvariantCulture, out var team)
                || team is < FirstTeam or > LastTeam)
                continue;

            var score = _scores[team].ToString(CultureInfo.InvariantCulture);
            if (board.Data.StateText != score)
                await furniture.UpdateAsync(board, board.Data with { State = score });
        }
    }

    private Task RaiseAsync(GameEvent e) => OnEvent?.Invoke(e) ?? Task.CompletedTask;

    /// <summary>The team types a wired team join chooses between (wiredfurni.params.team_type.N).</summary>
    public enum TeamType
    {
        Wired = 0,
        Banzai = 1,
        Freeze = 2,
    }

    public enum GameEventType
    {
        Started,
        Ended,
        ScoreChanged,
        ClockTicked,
    }

    public sealed record GameEvent(GameEventType Type, int Team = NoTeam, int OldScore = 0, int NewScore = 0, GameClock? Clock = null);
}

/// <summary>One clock of the room: a countdown game timer (with the preset times its custom params
/// list, in seconds) or a counter that counts up (no presets). Its time is kept in pulses of half a
/// second; the furni shows whole seconds.</summary>
public sealed class GameClock
{
    public required RoomItemStore.PlacedItem Item { get; init; }
    public required IReadOnlyList<int> PresetSeconds { get; init; }
    public bool CountsDown => PresetSeconds.Count > 0;

    public int PresetIndex { get; private set; }

    /// <summary>Time left (a countdown) or gone (a counter), in pulses of half a second.</summary>
    public int Pulses { get; set; }

    public bool Running { get; set; }

    /// <summary>Not started since it was last reset: starting it now starts a game.</summary>
    public bool Fresh { get; set; } = true;

    public static GameClock For(RoomItemStore.PlacedItem item)
    {
        var presets = item.Item.Definition!.CustomParams
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 0)
            .Where(s => s > 0)
            .ToList();

        // a clock placed with a time on it keeps it; otherwise it starts at its first preset
        var shown = int.TryParse(item.Data.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) ? seconds : 0;
        var index = Math.Max(0, presets.IndexOf(shown));
        return new GameClock
        {
            Item = item,
            PresetSeconds = presets,
            PresetIndex = index,
            Pulses = (presets.Count > 0 && shown == 0 ? presets[index] : shown) * 2,
        };
    }

    /// <summary>Back to its start: a countdown's current preset, a counter's zero.</summary>
    public void Restart()
    {
        Pulses = CountsDown ? PresetSeconds[PresetIndex] * 2 : 0;
        Fresh = true;
    }

    /// <summary>A countdown's next preset time; a counter back to zero.</summary>
    public void NextPreset()
    {
        if (CountsDown)
            PresetIndex = (PresetIndex + 1) % PresetSeconds.Count;
        Restart();
    }
}
