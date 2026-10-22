using Habbo.Domain.Moderation;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Chat;
using Habbo.Silo.Games.SnowWar;
using Habbo.Silo.Games.SnowWar.Arenas;
using Habbo.Silo.Games.SnowWar.Engine;
using Habbo.Silo.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Grains.Games;

/// <summary>One SnowStorm game. The players leave their rooms and load the arena; once they have (or the time to load
/// is up) the round counts down and starts, and the game runs turn by turn on the same engine as the clients, sending
/// each turn's checksum and the next turn's events. When time is up the results are shown, kept and counted towards
/// the players' achievements, and whoever wants to play again goes back to the directory together.</summary>
public sealed partial class SnowWarGameGrain(
    IOptionsMonitor<SnowWarOptions> options,
    SnowWarArenaCatalog arenas,
    SnowWarAccounts accounts,
    PlayerMessenger messenger,
    WordFilter wordFilter,
    ILogger<SnowWarGameGrain> logger) : Grain, ISnowWarGameGrain
{
    /// <summary>The longest arena chat line, as long as a line of room chat, and how often a player may say one.</summary>
    private const int MaxChatLength = RoomChatLine.TextMaxLength;
    private static readonly TimeSpan ChatInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<Guid, DateTimeOffset> _lastChat = [];

    /// <summary>The loading screen's "done".</summary>
    private const int FullyLoaded = 100;

    /// <summary>What the stage is called when it starts; the client does not look at it.</summary>
    private const string RoomType = "snowwar";

    private SnowWarOptions Rules => options.CurrentValue;

    private SnowWarGameSetup _setup = null!;
    private SnowWarArenaLevel _level = null!;
    private SnowWarArena _arena = null!;
    private SnowWarStage _stage = null!;
    private readonly SnowWarGameRules _stats = new();
    private readonly Dictionary<Guid, SnowWarPlayer> _players = [];
    private readonly Dictionary<Guid, HumanGameObject> _humans = [];
    private readonly HashSet<Guid> _loaded = [];
    private readonly HashSet<Guid> _gone = [];
    private readonly HashSet<Guid> _rematching = [];
    private SnowWarPhase _phase = SnowWarPhase.Over;
    private IGrainTimer? _timer;
    private int _nextObjectId;

    /// <summary>The players still in the arena.</summary>
    private IEnumerable<Guid> Present => _players.Keys.Where(id => !_gone.Contains(id));

    public async Task StartAsync(SnowWarGameSetup setup)
    {
        if (_phase != SnowWarPhase.Over || await arenas.ByFieldTypeAsync(setup.FieldType) is not { } level)
            return;

        _setup = setup;
        _level = level;
        foreach (var player in setup.Players)
            _players[player.Id] = player;
        BuildArena();
        _phase = SnowWarPhase.Loading;

        // the players leave the rooms they were in: the arena is where they are now
        foreach (var player in setup.Players.Where(p => p.RoomBeforeGame > 0))
            await GrainFactory.GetGrain<IGameRoomGrain>(player.RoomBeforeGame).LeaveAsync(player.Id);

        var enter = new Game2EnterArenaMessageComposer(SnowWarLobby.SnowWarGameType, level.FieldType, Rules.Teams,
            setup.Players.Select(p => new Game2PlayerData(p.AccountId, p.Name, p.Figure, p.Gender, p.Team)).ToList(), LevelToWire());
        await SendAsync(enter);
        await SendAsync(new Game2StageLoadMessageComposer(SnowWarLobby.SnowWarGameType));
        _timer = this.RegisterGrainTimer(async _ => await StartStageAsync(),
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(Rules.StageLoadTimeoutSeconds), Period = Timeout.InfiniteTimeSpan });
    }

    public async Task LoadedAsync(Guid playerId, int percentage)
    {
        if (_phase != SnowWarPhase.Loading || !_players.ContainsKey(playerId) || percentage < FullyLoaded || !_loaded.Add(playerId))
            return;

        var ready = _players.Values.Where(p => _loaded.Contains(p.Id)).Select(p => p.AccountId).ToList();
        await SendAsync(new Game2StageStillLoadingMessageComposer(ready.Count * FullyLoaded / _players.Count, ready));
        if (Present.All(_loaded.Contains))
            await StartStageAsync();
    }

    public Task MoveAsync(Guid playerId, int x, int y) => QueueAsync(new SnowWarAction(playerId, SnowWarActionKind.Move, X: x, Y: y));

    public Task MakeSnowballAsync(Guid playerId) => QueueAsync(new SnowWarAction(playerId, SnowWarActionKind.MakeSnowball));

    public Task ThrowAtHumanAsync(Guid playerId, int targetId, int trajectory) =>
        QueueAsync(new SnowWarAction(playerId, SnowWarActionKind.ThrowAtHuman, Target: targetId, Trajectory: trajectory));

    public Task ThrowAtPositionAsync(Guid playerId, int x, int y, int trajectory) =>
        QueueAsync(new SnowWarAction(playerId, SnowWarActionKind.ThrowAtPosition, X: x, Y: y, Trajectory: trajectory));

    public Task RequestFullStatusAsync(Guid playerId)
    {
        if (_phase == SnowWarPhase.Running && _players.ContainsKey(playerId))
            _fullStatusFor.Add(playerId);
        return Task.CompletedTask;
    }

    public async Task ChatAsync(Guid playerId, string message)
    {
        // said as room chat would be: by someone still in the arena, not too often, no longer than a line, through the filter
        var now = DateTimeOffset.UtcNow;
        if (!_players.TryGetValue(playerId, out var player) || _gone.Contains(playerId) || string.IsNullOrWhiteSpace(message)
            || (_lastChat.TryGetValue(playerId, out var last) && now - last < ChatInterval))
            return;

        _lastChat[playerId] = now;
        message = message.Trim();
        if (message.Length > MaxChatLength)
            message = message[..MaxChatLength];
        await SendAsync(new Game2GameChatFromPlayerMessageComposer(player.AccountId, (await wordFilter.FilterAsync(message)).Text));
    }

    public async Task ExitAsync(Guid playerId)
    {
        if (!_players.TryGetValue(playerId, out var player) || !_gone.Add(playerId))
            return;

        if (_humans.TryGetValue(playerId, out var human))
        {
            // off the field: before the stage is shown, at once; after, with the next turn, so every client sees it go
            if (_phase == SnowWarPhase.Loading)
                _stage.RemoveObject(human.Id);
            else if (_phase is SnowWarPhase.Countdown or SnowWarPhase.Running)
                _leaving.Add(human);
            await SendAsync(new Game2PlayerExitedGameArenaMessageComposer(player.AccountId, human.Id));
        }

        // walking out before the results is quitting, which keeps one from the next game a while
        if (_phase is SnowWarPhase.Results or SnowWarPhase.Over)
        {
            await Directory.GameOverAsync(_setup.GameId, [playerId]);
            return;
        }

        await Directory.QuitAsync(_setup.GameId, playerId);
        await RecordQuitAsync(player);

        // the one everyone else was waiting for may be the one who left
        if (_phase == SnowWarPhase.Loading && Present.Any() && Present.All(_loaded.Contains))
            await StartStageAsync();
    }

    public async Task PlayAgainAsync(Guid playerId)
    {
        if (_phase != SnowWarPhase.Results || !_players.TryGetValue(playerId, out var player) || !_rematching.Add(playerId))
            return;

        await SendAsync(new Game2PlayerRematchesMessageComposer(player.AccountId));
        var data = await GrainFactory.GetGrain<IPlayerGrain>(playerId).GetDataAsync();
        await Directory.RematchAsync(_setup.GameId, data);
    }

    private ISnowWarDirectoryGrain Directory => GrainFactory.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key);

    /// <summary>The arena as the engine plays it: its tiles and furni, its trees, piles and machines, and the players
    /// on their teams' starting tiles, facing the middle.</summary>
    private void BuildArena()
    {
        _arena = new SnowWarArena(Rules.Teams);
        _stage = new SnowWarStage(_arena, _level.Width, _level.Height, _level.HeightMap, _level.Furni.Select(f =>
            new SnowWarFuseObject(f.Id, f.X, f.Y, f.XDimension, f.YDimension, f.Height, f.Direction, f.CanStandOn)));
        _arena.Stage = _stage;

        var nextId = 1;
        foreach (var furni in _level.Furni)
        {
            if (furni.GameObject is not { } seed || _stage.TileAt(furni.X, furni.Y) is not { } tile)
                continue;

            var direction = Direction8.Get(furni.Direction) ?? Direction8.N;
            SnowWarGameObject? gameObject = seed.Type switch
            {
                SnowWarGameObjectSeed.Tree => new TreeGameObject(nextId, tile, direction, furni.Height, furni.Id, 0, seed.MaxHits),
                SnowWarGameObjectSeed.Pile => new SnowballPileGameObject(nextId, seed.Snowballs, seed.MaxSnowballs, tile, furni.Id),
                SnowWarGameObjectSeed.Machine => new SnowballMachineGameObject(nextId, seed.Snowballs, seed.MaxSnowballs, tile, furni.Id, direction),
                _ => null,
            };
            if (gameObject is null)
                continue;

            _stage.AddObject(gameObject);
            nextId++;
        }

        foreach (var team in _players.Values.GroupBy(p => p.Team))
        {
            var spawns = _level.Spawns.GetValueOrDefault(team.Key) ?? [];
            var index = 0;
            foreach (var player in team)
            {
                var tile = StartingTile(spawns, ref index);
                if (tile is null)
                {
                    logger.LogWarning("SnowStorm arena {FieldType} has no room left for {Player}", _level.FieldType, player.Name);
                    continue;
                }

                var facing = Direction360.ToDirection8(Direction360.AngleFromComponents(
                    (_level.Width / 2 - tile.X) * Tile.Width, (_level.Height / 2 - tile.Y) * Tile.Width));
                var human = new HumanGameObject(nextId++, player.AccountId, player.Team, player.Name, player.Figure, player.Gender, tile, facing, _stats);
                _stage.AddObject(human);
                _humans[player.Id] = human;
            }
        }

        _nextObjectId = nextId;
    }

    /// <summary>The next free starting tile of the team, or any free tile when they have run out.</summary>
    private Tile? StartingTile(IReadOnlyList<(int X, int Y)> spawns, ref int index)
    {
        while (index < spawns.Count)
        {
            var (x, y) = spawns[index++];
            if (_stage.TileAt(x, y) is { } tile && tile.CanMoveTo())
                return tile;
        }

        for (var y = 0; y < _stage.Height; y++)
        for (var x = 0; x < _stage.Width; x++)
        {
            if (_stage.TileAt(x, y) is { } tile && tile.CanMoveTo())
                return tile;
        }

        return null;
    }

    private GameLevelData LevelToWire() => new(_level.Width, _level.Height, _level.HeightMap, _level.Furni.Select(f => new FuseObjectData(
        f.ClassName, f.Id, f.X, f.Y, f.XDimension, f.YDimension, f.Height, f.Direction, f.Altitude, f.CanStandOn,
        FurniStuffData.From(FurniDataView.WithState(InitialState(f))))).ToList());

    /// <summary>What a furni shows at first: a pile or a machine how many snowballs it holds, a tree no hits yet.</summary>
    private static string InitialState(SnowWarLevelFurni furni) => furni.GameObject switch
    {
        { Type: SnowWarGameObjectSeed.Pile or SnowWarGameObjectSeed.Machine } seed => seed.Snowballs.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => "0",
    };

    private async Task StartStageAsync()
    {
        if (_phase != SnowWarPhase.Loading)
            return;

        _timer?.Dispose();
        if (Present.Count(_loaded.Contains) < Rules.MinPlayers)
        {
            await CancelAsync();
            return;
        }

        // who has not loaded by now is left behind: off the field before anyone sees it, and free to play elsewhere
        var behind = Present.Where(id => !_loaded.Contains(id)).ToList();
        foreach (var id in behind)
        {
            _gone.Add(id);
            if (_humans.TryGetValue(id, out var human))
                _stage.RemoveObject(human.Id);
        }
        if (behind.Count > 0)
            await Directory.GameOverAsync(_setup.GameId, behind);

        _phase = SnowWarPhase.Countdown;
        var countdown = Rules.StageCountdownSeconds;
        await SendAsync(new Game2StageStartingMessageComposer(SnowWarLobby.SnowWarGameType, RoomType, countdown, ObjectsToWire()));
        _timer = this.RegisterGrainTimer(async _ => await StartRunningAsync(),
            new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(countdown), Period = Timeout.InfiniteTimeSpan });
    }

    /// <summary>Too few made it into the arena for a game: it is called off, and everyone is free again.</summary>
    private async Task CancelAsync()
    {
        logger.LogInformation("SnowStorm game {GameId} is called off: {Loaded} of {Players} loaded the arena",
            _setup.GameId, Present.Count(_loaded.Contains), _players.Count);
        _phase = SnowWarPhase.Over;
        await SendAsync(new Habbo.Protocol.Messages.Outgoing.Game.Directory.Game2GameCancelledMessageComposer());
        await Directory.GameOverAsync(_setup.GameId, [.. _players.Keys]);
        await Directory.GameEndedAsync(_setup.GameId);
        DeactivateOnIdle();
    }

    private Task SendAsync(Habbo.Protocol.Composers.IComposer composer) => messenger.SendAsync(Present.ToList(), composer);
}
