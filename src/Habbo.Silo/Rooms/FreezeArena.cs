using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Rooms;

/// <summary>Freeze in one room, as measured on Habbo (docs/games.md).
///
/// A game starts every player on the lives the rules give (shown over the avatar) and their team on 40 points a
/// player, the tiles clear, the ice blocks whole and the exits lit. A player double-clicks a tile to drop a
/// snowball on the tile they stand on; it bursts two seconds later in a cross as far as their range, each tile a
/// step later than the one before. The burst breaks the first whole block on each arm into a power-up and stops
/// there, and hits the first player on each arm, stopping there too: a hit costs a life and the victim's team the
/// points a hit is worth, which a hit on another team wins the thrower's; one on a teammate costs the thrower's
/// team the rules' teammate points. A hit player is frozen for five seconds, then protected for five more. One out
/// of lives leaves their team for the exit; a game with one team left ends.
///
/// Stepping onto a broken block picks its power-up up: 2 one more tile of range, 3 one more snowball at once, 4 a
/// diagonal burst and 5 a burst in every direction across the arena (each for the next snowball), 6 a life back,
/// 7 a shield, and 1 nothing.</summary>
public sealed class FreezeArena(
    RoomGames games,
    RoomItemStore items,
    RoomEntityStore entities,
    IFurniRoom furniture,
    GameRoomActions? actions,
    Action<RoomEntityStore.RoomEntity, int> applyEffect)
{
    /// <summary>A team's points at the start, for each of its players (Habbo: 40 for one player).</summary>
    private const int PointsPerPlayer = 40;

    private static readonly TimeSpan FuseTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FrozenTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProtectedTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShieldTime = TimeSpan.FromSeconds(10);

    private const int MaxRange = 5;
    private const int MaxSnowballs = 5;

    /// <summary>How far a burst in every direction reaches: across any arena.</summary>
    private const int MegaRange = 100;

    // a tile's states: a snowball (by its size), a burst; a block's: whole, a power-up's type, picked up (+10)
    private const int Clear = 0;
    private const int Snowball = 1;
    private const int BigSnowball = 2;
    private const int Burst = 11;
    private const int PickedUp = 10;

    private const int NoPowerUp = 1;
    private const int RangePowerUp = 2;
    private const int SnowballPowerUp = 3;
    private const int DiagonalPowerUp = 4;
    private const int MegaPowerUp = 5;
    private const int LifePowerUp = 6;
    private const int ShieldPowerUp = 7;

    /// <summary>Habbo's odds of each power-up (types 1–7, cumulative), for blocks whose custom params do not say.</summary>
    private static readonly double[] HabboPowerUpOdds = [0.4, 0.6, 0.7, 0.75, 0.8, 0.9, 1.0];

    private static readonly (int Dx, int Dy)[] Straight = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int Dx, int Dy)[] Diagonal = [(1, 1), (-1, -1), (-1, 1), (1, -1)];

    private readonly Dictionary<RoomEntityStore.RoomEntity, Player> _players = [];
    private readonly HashSet<RoomItemStore.PlacedItem> _snowballs = [];
    private FreezeRules _rules = FreezeRules.Habbo;

    /// <summary>Whether the game on is one of Freeze: a Freeze counter started it.</summary>
    private bool _playing;

    private sealed class Player
    {
        public int Lives { get; set; }
        public int Range { get; set; } = 1;
        public int Snowballs { get; set; } = 1;
        public int Flying { get; set; }
        public bool NextDiagonal { get; set; }
        public bool NextMega { get; set; }
        public bool Frozen { get; set; }
        public bool Protected { get; set; }

        /// <summary>The team's effect, worn again once thawed and unprotected.</summary>
        public int TeamEffect { get; init; }

        /// <summary>Counts freezes and shields, so a timer from an earlier one leaves a newer one alone.</summary>
        public int Spell { get; set; }
    }

    public static bool IsTile(RoomItemStore.PlacedItem item) => item.Interaction is FreezeTileInteraction;

    /// <summary>Whether a clock is a Freeze counter (es_counter), whose start makes the game one of Freeze.</summary>
    public static bool IsCounter(RoomItemStore.PlacedItem clock) =>
        clock.Item.Definition!.ClassName.StartsWith("es_", StringComparison.Ordinal);

    private static bool IsBlock(RoomItemStore.PlacedItem item) => item.Interaction is FreezeBlockInteraction;

    /// <summary>Whether a double-click on the item throws a snowball: a tile, or a block (Habbo threw on the
    /// player's own tile for a click on a block beside them).</summary>
    public static bool Throws(RoomItemStore.PlacedItem item) => IsTile(item) || IsBlock(item);

    private bool HasArena => items.Items.Any(IsTile);

    private RoomItemStore.PlacedItem? TileAt(int x, int y) => items.FloorItemsOn(x, y).FirstOrDefault(IsTile);

    private RoomItemStore.PlacedItem? BlockAt(int x, int y) => items.FloorItemsOn(x, y).FirstOrDefault(IsBlock);

    private static int StateOf(RoomItemStore.PlacedItem item) =>
        int.TryParse(item.Data.StateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ? state : Clear;

    /// <summary>A state the client shows after a delay: state × 1000 + milliseconds.</summary>
    private static string Delayed(int state, TimeSpan delay = default) =>
        (state * 1000 + (int)delay.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);

    private static int Shown(int delayedState) => delayedState / 1000;

    /// <summary>A game starts: the arena is cleared for it, and, a game of Freeze, its players set up.</summary>
    public async Task StartAsync(bool freeze)
    {
        _players.Clear();
        _snowballs.Clear();
        _playing = freeze && HasArena;
        if (!HasArena)
            return;

        _rules = FreezeRules.Parse(items.Items.First(IsTile).Item.Definition!.CustomParams);
        await furniture.BatchedAsync(async () =>
        {
            foreach (var item in items.Items.ToList())
            {
                if (IsTile(item) || IsBlock(item))
                    await furniture.UpdateAsync(item, item.Data with { State = Delayed(Clear) }, persist: false);
                else if (item.Interaction is FreezeExitInteraction)
                    await furniture.UpdateAsync(item, item.Data with { State = "1" }, persist: false);
            }
        });

        if (!_playing)
            return;

        foreach (var user in entities.Entities.Where(e => e.Team != RoomGames.NoTeam).ToList())
        {
            PlayerOf(user);
            await games.AddScoreAsync(user.Team, PointsPerPlayer);
        }
    }

    public async Task EndAsync()
    {
        foreach (var (user, player) in _players.ToList())
        {
            player.Spell++;
            if (player.Frozen)
                actions?.SetFrozen(user, false);
            if ((player.Frozen || player.Protected) && user.Team != RoomGames.NoTeam)
                applyEffect(user, player.TeamEffect);
        }

        _players.Clear();
        _snowballs.Clear();
        var played = _playing;
        _playing = false;
        if (!played)
            return;

        await furniture.BatchedAsync(async () =>
        {
            foreach (var item in items.Items.ToList())
            {
                // power-ups nobody picked up go as if picked up; the exits go dark
                if (IsBlock(item) && Shown(StateOf(item)) is >= NoPowerUp and <= ShieldPowerUp and var type)
                    await furniture.UpdateAsync(item, item.Data with { State = Delayed(type + PickedUp) });
                else if (item.Interaction is FreezeExitInteraction)
                    await furniture.UpdateAsync(item, item.Data with { State = "0" });
            }
        });
    }

    private Player PlayerOf(RoomEntityStore.RoomEntity user)
    {
        if (_players.TryGetValue(user, out var player))
            return player;

        player = new Player { Lives = _rules.Lives, TeamEffect = user.EffectId };
        _players[user] = player;
        furniture.Broadcast(new GamePlayerValueMessageComposer(user.RoomIndex, player.Lives));
        return player;
    }

    // ---------- snowballs ----------

    public async Task ThrowAsync(RoomEntityStore.RoomEntity user)
    {
        if (!_playing || user.Team == RoomGames.NoTeam || TileAt(user.X, user.Y) is not { } tile || _snowballs.Contains(tile))
            return;

        var player = PlayerOf(user);
        if (player.Frozen || player.Lives <= 0 || player.Flying >= player.Snowballs)
            return;

        var (range, diagonal, mega) = (player.NextMega ? MegaRange : player.Range, player.NextDiagonal, player.NextMega);
        player.NextDiagonal = player.NextMega = false;
        player.Flying++;
        _snowballs.Add(tile);
        await furniture.UpdateAsync(tile, tile.Data with { State = Delayed(range > 1 ? BigSnowball : Snowball) }, persist: false);
        furniture.Schedule(FuseTime, () => BurstAsync(tile, user, player, range, diagonal, mega));
    }

    private async Task BurstAsync(RoomItemStore.PlacedItem tile, RoomEntityStore.RoomEntity thrower, Player player, int range, bool diagonal, bool mega)
    {
        _snowballs.Remove(tile);
        player.Flying = Math.Max(0, player.Flying - 1);
        if (!_playing || !furniture.IsPlaced(tile))
            return;

        var directions = mega ? Straight.Concat(Diagonal).ToArray() : diagonal ? Diagonal : Straight;
        var victims = new List<RoomEntityStore.RoomEntity>();
        await furniture.BatchedAsync(async () =>
        {
            await BlastAsync(tile.X, tile.Y, 0, victims);
            foreach (var (dx, dy) in directions)
            {
                for (var distance = 1; distance <= range; distance++)
                {
                    if (!await BlastAsync(tile.X + dx * distance, tile.Y + dy * distance, distance, victims))
                        break;
                }
            }
        });

        foreach (var victim in victims.Distinct())
            await HitAsync(victim, thrower);
    }

    /// <summary>The burst reaching a tile: false when it stops there (no tile, a whole block, a player).</summary>
    private async Task<bool> BlastAsync(int x, int y, int distance, List<RoomEntityStore.RoomEntity> victims)
    {
        if (TileAt(x, y) is not { } tile)
            return false;

        var delay = _rules.BlastStepTime * Math.Max(1, distance);
        await furniture.UpdateAsync(tile, tile.Data with { State = Delayed(Burst, delay) }, persist: false);

        var goesOn = true;
        if (BlockAt(x, y) is { } block && FreezeBlockInteraction.Whole(block.Data))
        {
            await furniture.UpdateAsync(block, block.Data with { State = Delayed(PowerUp(block), delay) }, persist: false);
            goesOn = false;
        }

        foreach (var user in entities.Entities.Where(e => e.X == x && e.Y == y && e.Team != RoomGames.NoTeam))
        {
            var player = PlayerOf(user);
            if (player.Frozen || player.Protected || player.Lives <= 0)
                continue;
            victims.Add(user);
            goesOn = false;
        }

        return goesOn;
    }

    private static int PowerUp(RoomItemStore.PlacedItem block)
    {
        var odds = block.Item.Definition!.CustomParams.Split(',')
            .Select(p => double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN)
            .ToArray();
        if (odds.Length == 0 || odds.Any(double.IsNaN))
            odds = HabboPowerUpOdds;

        var roll = Random.Shared.NextDouble() * odds[^1];
        var index = Array.FindIndex(odds, threshold => roll < threshold);
        return NoPowerUp + (index < 0 ? odds.Length - 1 : index);
    }

    private async Task HitAsync(RoomEntityStore.RoomEntity victim, RoomEntityStore.RoomEntity thrower)
    {
        var player = PlayerOf(victim);
        player.Lives--;
        furniture.Broadcast(new GamePlayerValueMessageComposer(victim.RoomIndex, player.Lives));

        var team = victim.Team;
        await games.AddScoreAsync(team, -_rules.HitPoints);
        if (thrower != victim && thrower.Team != RoomGames.NoTeam)
            await games.AddScoreAsync(thrower.Team, thrower.Team == team ? _rules.TeammateHitPoints : _rules.HitPoints);

        if (player.Lives <= 0)
        {
            await OutAsync(victim);
            return;
        }

        var spell = ++player.Spell;
        player.Frozen = true;
        actions?.SetFrozen(victim, true);
        applyEffect(victim, FreezeRules.ForTeam(_rules.FrozenEffects, team));
        furniture.Schedule(FrozenTime, () =>
        {
            if (player.Spell != spell || !_players.ContainsKey(victim))
                return Task.CompletedTask;
            player.Frozen = false;
            actions?.SetFrozen(victim, false);
            Protect(victim, player, team, ProtectedTime);
            return Task.CompletedTask;
        });
    }

    private void Protect(RoomEntityStore.RoomEntity user, Player player, int team, TimeSpan time)
    {
        var spell = ++player.Spell;
        player.Protected = true;
        applyEffect(user, FreezeRules.ForTeam(_rules.ProtectionEffects, team));
        furniture.Schedule(time, () =>
        {
            if (player.Spell == spell && _players.ContainsKey(user))
            {
                player.Protected = false;
                applyEffect(user, player.TeamEffect);
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>Out of lives: off the team and to an exit; the last team standing wins.</summary>
    private async Task OutAsync(RoomEntityStore.RoomEntity user)
    {
        _players.Remove(user);
        await games.LeaveAsync(user);
        if (items.Items.FirstOrDefault(item => item.Interaction is FreezeExitInteraction) is { } exit)
            actions?.Warp(user, exit.X, exit.Y);

        var teamsLeft = entities.Entities.Where(e => e.Team != RoomGames.NoTeam).Select(e => e.Team).Distinct().Count();
        if (_playing && teamsLeft <= 1)
            await games.FinishAsync();
    }

    // ---------- power-ups ----------

    /// <summary>A player stepped onto an item: a broken block's power-up is theirs.</summary>
    public async Task SteppedOnAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item)
    {
        if (!IsBlock(item) || !_playing || user.Team == RoomGames.NoTeam
            || Shown(StateOf(item)) is not (>= NoPowerUp and <= ShieldPowerUp and var type))
            return;

        await furniture.UpdateAsync(item, item.Data with { State = Delayed(type + PickedUp) }, persist: false);
        actions?.ProgressAchievement(user, AchievementNames.FreezePowerUp, 1);

        var player = PlayerOf(user);
        switch (type)
        {
            case RangePowerUp:
                player.Range = Math.Min(MaxRange, player.Range + 1);
                break;
            case SnowballPowerUp:
                player.Snowballs = Math.Min(MaxSnowballs, player.Snowballs + 1);
                break;
            case DiagonalPowerUp:
                player.NextDiagonal = true;
                break;
            case MegaPowerUp:
                player.NextMega = true;
                break;
            case LifePowerUp when player.Lives < _rules.Lives:
                player.Lives++;
                furniture.Broadcast(new GamePlayerValueMessageComposer(user.RoomIndex, player.Lives));
                break;
            case ShieldPowerUp when !player.Frozen:
                Protect(user, player, user.Team, ShieldTime);
                break;
        }
    }
}
