using Habbo.Protocol.Messages.Outgoing.Game.Directory;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Games.SnowWar;
using Habbo.Silo.Games.SnowWar.Arenas;
using Habbo.Silo.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Grains.Games;

/// <summary>The game center's SnowStorm directory. Players wait in lobbies, balanced between the teams; once enough
/// wait the countdown runs, and when it ends the game starts in a game grain of its own. The directory remembers which
/// game each player is in, and the room they came from, until the game is over. When the hotel runs only so many games
/// at once, lobbies whose countdown ended wait their turn for an arena; and one who walks out of a game waits a while
/// before playing again.</summary>
public sealed class SnowWarDirectoryGrain(
    IOptionsMonitor<SnowWarOptions> options,
    SnowWarAccounts accounts,
    SnowWarArenaCatalog arenas,
    PlayerMessenger messenger,
    ILogger<SnowWarDirectoryGrain> logger) : Grain, ISnowWarDirectoryGrain
{
    /// <summary>The directory's statuses: open, or closed to all.</summary>
    private const int Open = 0;
    private const int Closed = 1;

    /// <summary>Why joining failed, as the client explains it: already in a game, or no free games left.</summary>
    private const int HasActiveInstance = 6;
    private const int NoFreeGamesLeft = 8;

    private readonly Dictionary<int, SnowWarLobby> _lobbies = [];
    private readonly Dictionary<Guid, int> _gameOf = [];
    private readonly Dictionary<Guid, int> _roomBeforeGame = [];
    private readonly Dictionary<Guid, int> _lastGameOf = [];
    private readonly HashSet<int> _running = [];
    private readonly List<SnowWarLobby> _arenaQueue = [];
    private readonly Dictionary<Guid, DateTime> _blockedUntil = [];
    /// <summary>The last game id given out. It starts from the clock, so ids from before a restart are not given again
    /// to a game grain that may still be showing that game's results.</summary>
    private int _nextGameId = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private SnowWarOptions Rules => options.CurrentValue;

    /// <summary>How often the directory looks whether it holds anything, and how long it then stays up for it.</summary>
    private static readonly TimeSpan BusyCheck = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan KeptWhileBusy = TimeSpan.FromMinutes(5);

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // lobbies, games running and quit blocks live only here, and a timer does not keep a grain up: a lone player
        // waiting in a lobby for longer than the collection age would lose the lobby, and a running game's end would
        // reach a directory that forgot it
        this.RegisterGrainTimer(_ =>
            {
                if (_lobbies.Count > 0 || _running.Count > 0 || _gameOf.Count > 0 || _arenaQueue.Count > 0 || _blockedUntil.Count > 0)
                    DelayDeactivation(KeptWhileBusy);
                return Task.CompletedTask;
            },
            new GrainTimerCreationOptions { DueTime = BusyCheck, Period = BusyCheck });
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task SendStatusAsync(PlayerData player)
    {
        if (!Rules.Enabled)
        {
            await messenger.SendAsync(player.Id, new Game2GameDirectoryStatusMessageComposer(Closed, 0, 0, 0));
            return;
        }

        var (played, freeLeft) = await accounts.GamesAsync(player);
        await messenger.SendAsync(player.Id, new Game2GameDirectoryStatusMessageComposer(Open, 0, played, freeLeft));
        if (BlockLeft(player.Id) is { } block)
            await messenger.SendAsync(player.Id, new Game2UserBlockedMessageComposer(block));
    }

    public async Task SendAccountStatusAsync(PlayerData player, int gameType)
    {
        var (played, freeLeft) = await accounts.GamesAsync(player);
        await messenger.SendAsync(player.Id, new Game2AccountGameStatusMessageComposer(gameType, freeLeft, played));
    }

    public Task SendTokenOffersAsync(PlayerData player) =>
        messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Catalog.SnowWarGameTokensMessageComposer(Rules.TokenOffers
            .Select(o => new Habbo.Protocol.Messages.Outgoing.Catalog.SnowWarGameTokenOfferData(o.Id, o.LocalizationId, o.PriceInCredits,
                o.PriceInActivityPoints, o.ActivityPointType)).ToList()));

    public async Task BuyTokensAsync(PlayerData player, int offerId)
    {
        if (Rules.TokenOffers.FirstOrDefault(o => o.Id == offerId) is not { } offer)
        {
            await messenger.SendAsync(player.Id, Habbo.Protocol.Messages.Outgoing.Catalog.PurchaseErrorMessageComposer.Failed());
            return;
        }

        if (await accounts.BuyTokensAsync(player, offer) is { } shortOfCredits)
        {
            await messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Catalog.NotEnoughBalanceMessageComposer(
                shortOfCredits, !shortOfCredits, offer.ActivityPointType));
            return;
        }

        await messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Catalog.PurchaseOKMessageComposer(
            new Habbo.Protocol.Messages.Outgoing.Catalog.PurchaseOfferData(offer.Id, offer.LocalizationId, false, offer.PriceInCredits,
                offer.PriceInActivityPoints, offer.ActivityPointType, false, [], 0, false), []));
        await SendAccountStatusAsync(player, SnowWarLobby.SnowWarGameType);
    }

    public async Task QuickJoinAsync(PlayerData player)
    {
        if (!await MayJoinAsync(player))
            return;

        var lobby = _lobbies.Values.FirstOrDefault(l => l.RematchOf is null && l.Players.Count < Rules.MaxPlayers)
                    ?? await NewLobbyAsync(player, fieldType: null, rematchOf: null);
        if (lobby is not null)
            await JoinAsync(lobby, player);
    }

    public async Task StartAsync(PlayerData player, string levelName)
    {
        if (!await MayJoinAsync(player))
            return;

        var fieldType = int.TryParse(levelName, out var named) ? named : (int?)null;
        if (await NewLobbyAsync(player, fieldType, rematchOf: null) is { } lobby)
            await JoinAsync(lobby, player);
    }

    public async Task RematchAsync(int previousGameId, PlayerData player)
    {
        if (_gameOf.ContainsKey(player.Id) || !Rules.Enabled || !await NotBlockedAsync(player.Id))
            return;

        var (_, freeLeft) = await accounts.GamesAsync(player);
        if (freeLeft == 0)
        {
            await messenger.SendAsync(player.Id, new Game2JoiningGameFailedMessageComposer(NoFreeGamesLeft));
            return;
        }

        var lobby = _lobbies.Values.FirstOrDefault(l => l.RematchOf == previousGameId && l.Players.Count < Rules.MaxPlayers)
                    ?? await NewLobbyAsync(player, fieldType: null, rematchOf: previousGameId);
        if (lobby is null)
            return;

        await messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena.Game2GameRejoinMessageComposer(
            _roomBeforeGame.GetValueOrDefault(player.Id, -1)));
        await JoinAsync(lobby, player);
    }

    public async Task LeaveAsync(Guid playerId)
    {
        if (!_gameOf.TryGetValue(playerId, out var gameId) || !_lobbies.TryGetValue(gameId, out var lobby))
            return;

        var player = lobby.Players.First(p => p.Id == playerId);
        lobby.Players.Remove(player);
        _gameOf.Remove(playerId);
        await messenger.SendAsync(lobby.Players.Select(p => p.Id), new Game2UserLeftGameMessageComposer(player.AccountId));

        if (lobby.Players.Count == 0)
        {
            lobby.Countdown?.Dispose();
            _lobbies.Remove(gameId);
            _arenaQueue.Remove(lobby);
            await SendQueuePositionsAsync();
        }
        else if (lobby.Players.Count < Rules.MinPlayers && lobby.Countdown is not null)
        {
            lobby.Countdown.Dispose();
            lobby.Countdown = null;
            await messenger.SendAsync(lobby.Players.Select(p => p.Id), new Game2StopCounterMessageComposer());
        }
    }

    public Task<int?> GameOfAsync(Guid playerId) =>
        Task.FromResult(_gameOf.TryGetValue(playerId, out var gameId) ? gameId : (int?)null);

    public Task<int?> LastGameOfAsync(Guid playerId) =>
        Task.FromResult(_gameOf.TryGetValue(playerId, out var gameId) || _lastGameOf.TryGetValue(playerId, out gameId) ? gameId : (int?)null);

    public Task GameOverAsync(int gameId, IReadOnlyList<Guid> players)
    {
        foreach (var player in players.Where(p => _gameOf.GetValueOrDefault(p) == gameId))
        {
            _gameOf.Remove(player);
            _lastGameOf[player] = gameId;
        }

        return Task.CompletedTask;
    }

    public async Task QuitAsync(int gameId, Guid playerId)
    {
        await GameOverAsync(gameId, [playerId]);
        if (Rules.QuitBlockSeconds <= 0)
            return;

        _blockedUntil[playerId] = DateTime.UtcNow.AddSeconds(Rules.QuitBlockSeconds);
        await messenger.SendAsync(playerId, new Game2UserBlockedMessageComposer(Rules.QuitBlockSeconds));
    }

    public async Task GameEndedAsync(int gameId)
    {
        if (!_running.Remove(gameId))
            return;

        // blocks from quitting that have run out go, so the directory does not keep everyone who ever quit
        var now = DateTime.UtcNow;
        foreach (var lifted in _blockedUntil.Where(b => b.Value <= now).Select(b => b.Key).ToList())
            _blockedUntil.Remove(lifted);

        // the arena is free: the lobby that has waited longest takes it
        while (_arenaQueue.Count > 0 && !AtCapacity)
        {
            var next = _arenaQueue[0];
            _arenaQueue.RemoveAt(0);
            await StartGameAsync(next);
        }

        await SendQueuePositionsAsync();
    }

    private bool AtCapacity => Rules.MaxRunningGames > 0 && _running.Count >= Rules.MaxRunningGames;

    /// <summary>The seconds the player still has to wait to play, if they walked out of a game lately.</summary>
    private int? BlockLeft(Guid playerId)
    {
        if (!_blockedUntil.TryGetValue(playerId, out var until))
            return null;

        var left = (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds);
        if (left > 0)
            return left;

        _blockedUntil.Remove(playerId);
        return null;
    }

    private async Task<bool> NotBlockedAsync(Guid playerId)
    {
        if (BlockLeft(playerId) is not { } left)
            return true;

        await messenger.SendAsync(playerId, new Game2UserBlockedMessageComposer(left));
        return false;
    }

    /// <summary>Tells each lobby waiting for an arena where it is in the queue.</summary>
    private async Task SendQueuePositionsAsync()
    {
        for (var i = 0; i < _arenaQueue.Count; i++)
            await messenger.SendAsync(_arenaQueue[i].Players.Select(p => p.Id), new Game2InArenaQueueMessageComposer(i + 1));
    }

    /// <summary>The directory is open, the player is not in a game already nor waiting after walking out of one, and they
    /// have a game left.</summary>
    private async Task<bool> MayJoinAsync(PlayerData player)
    {
        if (!Rules.Enabled || !await NotBlockedAsync(player.Id))
            return false;

        if (_gameOf.ContainsKey(player.Id))
        {
            await messenger.SendAsync(player.Id, new Game2JoiningGameFailedMessageComposer(HasActiveInstance));
            return false;
        }

        var (_, freeLeft) = await accounts.GamesAsync(player);
        if (freeLeft != 0)
            return true;

        await messenger.SendAsync(player.Id, new Game2JoiningGameFailedMessageComposer(NoFreeGamesLeft));
        return false;
    }

    /// <summary>A new lobby on the arena asked for, or one picked at random; none when the hotel has no arenas.</summary>
    private async Task<SnowWarLobby?> NewLobbyAsync(PlayerData owner, int? fieldType, int? rematchOf)
    {
        var all = await arenas.AllAsync();
        if (all.Count == 0)
        {
            logger.LogWarning("SnowStorm has no arenas to play on; seed/snowwar_arenas.json is missing or empty");
            await messenger.SendAsync(owner.Id, new Game2StartingGameFailedMessageComposer(0));
            return null;
        }

        var arena = all.FirstOrDefault(a => a.FieldType == fieldType) ?? all[Random.Shared.Next(all.Count)];
        var lobby = new SnowWarLobby(++_nextGameId, arena.FieldType, owner.Username, rematchOf);
        _lobbies[lobby.GameId] = lobby;
        return lobby;
    }

    private async Task JoinAsync(SnowWarLobby lobby, PlayerData player)
    {
        var roomBeforeGame = await GrainFactory.GetGrain<IPlayerGrain>(player.Id).GetCurrentRoomAsync() ?? _roomBeforeGame.GetValueOrDefault(player.Id, -1);
        _roomBeforeGame[player.Id] = roomBeforeGame;
        var joined = await accounts.PlayerAsync(player, lobby.NextTeam(Rules.Teams), roomBeforeGame);
        var others = lobby.Players.Select(p => p.Id).ToList();
        lobby.Players.Add(joined);
        _gameOf[player.Id] = lobby.GameId;

        var data = lobby.ToWire(Rules.MaxPlayers, Rules.Teams);
        await messenger.SendAsync(player.Id, lobby.Players.Count == 1 && lobby.RematchOf is null
            ? new Game2GameCreatedMessageComposer(data)
            : new Game2GameLongDataMessageComposer(data));
        await messenger.SendAsync(others, new Game2UserJoinedGameMessageComposer(SnowWarLobby.PlayerToWire(joined), false));

        if (_arenaQueue.IndexOf(lobby) is var place and >= 0)
        {
            await messenger.SendAsync(player.Id, new Game2InArenaQueueMessageComposer(place + 1));
        }
        else if (lobby.Countdown is null && lobby.Players.Count >= Rules.MinPlayers)
        {
            var seconds = Rules.LobbyCountdownSeconds;
            lobby.Countdown = this.RegisterGrainTimer(async _ => await StartGameAsync(lobby),
                new GrainTimerCreationOptions { DueTime = TimeSpan.FromSeconds(seconds), Period = Timeout.InfiniteTimeSpan });
            await messenger.SendAsync(lobby.Players.Select(p => p.Id), new Game2StartCounterMessageComposer(seconds));
        }
        else if (lobby.Countdown is not null)
        {
            await messenger.SendAsync(player.Id, new Game2StartCounterMessageComposer(Rules.LobbyCountdownSeconds));
        }
    }

    private async Task StartGameAsync(SnowWarLobby lobby)
    {
        lobby.Countdown?.Dispose();
        lobby.Countdown = null;
        if (!_lobbies.ContainsKey(lobby.GameId) || lobby.Players.Count < Rules.MinPlayers)
            return;

        if (AtCapacity)
        {
            if (!_arenaQueue.Contains(lobby))
                _arenaQueue.Add(lobby);
            await SendQueuePositionsAsync();
            return;
        }

        _lobbies.Remove(lobby.GameId);
        _running.Add(lobby.GameId);

        // the room each player is in now, not the one they joined from: a player may have walked on while the lobby
        // filled, and the game takes them out of (and later back to) the room they are actually in
        for (var i = 0; i < lobby.Players.Count; i++)
        {
            var player = lobby.Players[i];
            if (await GrainFactory.GetGrain<IPlayerGrain>(player.Id).GetCurrentRoomAsync() is { } room && room != player.RoomBeforeGame)
            {
                lobby.Players[i] = player with { RoomBeforeGame = room };
                _roomBeforeGame[player.Id] = room;
            }
        }

        await messenger.SendAsync(lobby.Players.Select(p => p.Id),
            new Game2GameStartedMessageComposer(lobby.ToWire(Rules.MaxPlayers, Rules.Teams)));
        logger.LogInformation("SnowStorm game {GameId} starts on field {FieldType} with {Players} players",
            lobby.GameId, lobby.FieldType, lobby.Players.Count);
        await GrainFactory.GetGrain<ISnowWarGameGrain>(lobby.GameId)
            .StartAsync(new SnowWarGameSetup(lobby.GameId, lobby.FieldType, [.. lobby.Players]));
    }
}
