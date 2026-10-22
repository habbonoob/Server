using System.Globalization;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Rooms;

/// <summary>Battle Banzai in one room, as measured on Habbo (docs/games.md): its tiles and its puck.
///
/// A tile's state is 0 off, 1 lit for a game, then 3 × team + level: each step of a team's player raises it a
/// level, the third locks it and scores the team a point. A step on another team's unlocked tile makes it the
/// stepper's first level. Locking can close off an area: tiles held in by the team's locked tiles (orthogonally;
/// the diagonals need not be locked) are locked with it, a point each. Changes are shown once per room tick,
/// together.
///
/// Walking into the puck kicks it ahead as far as its custom params' first value, slowing down; during a game it
/// colours each tile it lands on for the kicker's team.</summary>
public sealed class BanzaiArena(RoomGames games, RoomItemStore items, IFurniRoom furniture, GameRoomActions? actions)
{
    private const int Off = 0;
    private const int Lit = 1;
    private const int LevelsPerTeam = 3;
    private const int LockedLevel = LevelsPerTeam - 1;

    /// <summary>How far a kick sends the puck when its custom params do not say.</summary>
    private const int DefaultKickPower = 6;

    /// <summary>How long the puck takes over its last tile; each tile before goes faster, by the power left.</summary>
    private static readonly TimeSpan LastStepTime = TimeSpan.FromMilliseconds(500);

    /// <summary>The winners' locked tiles flash off and on this many times when a game ends.</summary>
    private const int EndFlashes = 5;

    private static readonly TimeSpan FlashTime = TimeSpan.FromMilliseconds(500);

    /// <summary>Tile states changed since the last tick, shown together then.</summary>
    private readonly Dictionary<RoomItemStore.PlacedItem, int> _pending = [];

    /// <summary>Points scored since the last tick, by team, shown with the tiles.</summary>
    private readonly Dictionary<int, int> _pendingPoints = [];

    private readonly HashSet<int> _rollingPucks = [];

    public static bool IsTile(RoomItemStore.PlacedItem item) => item.Interaction is BanzaiTileInteraction;

    private IEnumerable<RoomItemStore.PlacedItem> Tiles => items.Items.Where(IsTile);

    private static int TeamOf(int state) => state >= LevelsPerTeam ? state / LevelsPerTeam : 0;

    private static bool IsLocked(int state) => TeamOf(state) != 0 && state % LevelsPerTeam == LockedLevel;

    private int StateOf(RoomItemStore.PlacedItem tile) =>
        _pending.TryGetValue(tile, out var pending)
            ? pending
            : int.TryParse(tile.Data.StateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ? state : Off;

    private RoomItemStore.PlacedItem? TileAt(int x, int y) => items.FloorItemsOn(x, y).FirstOrDefault(IsTile);

    /// <summary>A game starts: every tile lights up.</summary>
    public async Task StartAsync()
    {
        _pending.Clear();
        _pendingPoints.Clear();
        await furniture.BatchedAsync(async () =>
        {
            foreach (var tile in Tiles.ToList())
                await furniture.UpdateAsync(tile, tile.Data with { State = Lit.ToString(CultureInfo.InvariantCulture) }, persist: false);
        });
    }

    /// <summary>A game ends: tiles never stepped on go dark, and the winners' locked tiles flash, ending lit.</summary>
    public async Task EndAsync(IReadOnlyCollection<int> winners)
    {
        await FlushAsync();
        var tiles = Tiles.ToList();
        await furniture.BatchedAsync(async () =>
        {
            foreach (var tile in tiles.Where(tile => StateOf(tile) == Lit))
                await furniture.UpdateAsync(tile, tile.Data with { State = Off.ToString(CultureInfo.InvariantCulture) });
        });

        var won = tiles.Where(tile => IsLocked(StateOf(tile)) && winners.Contains(TeamOf(StateOf(tile))))
            .Select(tile => (Tile: tile, State: StateOf(tile)))
            .ToList();
        for (var flash = 0; flash < EndFlashes * 2; flash++)
        {
            var dark = flash % 2 == 0;
            var last = flash == EndFlashes * 2 - 1;
            furniture.Schedule(FlashTime * (flash + 1), () => furniture.BatchedAsync(async () =>
            {
                foreach (var (tile, state) in won.Where(won => furniture.IsPlaced(won.Tile)))
                    await furniture.UpdateAsync(tile, tile.Data with { State = (dark ? Off : state).ToString(CultureInfo.InvariantCulture) }, persist: last);
            }));
        }
    }

    /// <summary>A user stepped onto an item, coming from a tile: a tile is coloured for their team, the puck kicked.</summary>
    public void SteppedOn(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item, int fromX, int fromY)
    {
        if (IsTile(item) && games.Running && user.Team != RoomGames.NoTeam)
            Colour(item, user.Team, user);
        else if (item.Interaction is BanzaiPuckInteraction)
            Kick(item, user, Math.Sign(item.X - fromX), Math.Sign(item.Y - fromY));
    }

    /// <summary>The room's tick: shows every tile changed since the last one, in one message.</summary>
    public Task PulseAsync() => FlushAsync();

    private async Task FlushAsync()
    {
        if (_pending.Count == 0 && _pendingPoints.Count == 0)
            return;

        var changes = _pending.ToList();
        var points = _pendingPoints.ToList();
        _pending.Clear();
        _pendingPoints.Clear();
        await furniture.BatchedAsync(async () =>
        {
            foreach (var (tile, state) in changes.Where(change => furniture.IsPlaced(change.Key)))
                await furniture.UpdateAsync(tile, tile.Data with { State = state.ToString(CultureInfo.InvariantCulture) }, persist: false);
            foreach (var (team, scored) in points)
                await games.AddScoreAsync(team, scored);
        });
    }

    /// <summary>A step (or the puck) on a tile for a team: up a level, or the team's first level on a tile that was
    /// not theirs; locked tiles stay as they are.</summary>
    private void Colour(RoomItemStore.PlacedItem tile, int team, RoomEntityStore.RoomEntity? by)
    {
        var state = StateOf(tile);
        if (IsLocked(state))
            return;

        var coloured = TeamOf(state) == team ? state + 1 : team * LevelsPerTeam;
        _pending[tile] = coloured;
        if (IsLocked(coloured))
            Lock(tile, team, by);
    }

    private void Lock(RoomItemStore.PlacedItem tile, int team, RoomEntityStore.RoomEntity? by)
    {
        _pending[tile] = team * LevelsPerTeam + LockedLevel;
        Scored(team, by, 1);
        foreach (var (dx, dy) in Neighbours)
        {
            if (TileAt(tile.X + dx, tile.Y + dy) is not { } next || IsLocked(StateOf(next)))
                continue;

            if (ClosedOff(next) is { } area)
            {
                foreach (var held in area)
                    _pending[held] = team * LevelsPerTeam + LockedLevel;
                Scored(team, by, area.Count);
            }
        }
    }

    /// <summary>The tiles reachable from <paramref name="start"/> without crossing a locked tile, if none of them is
    /// at the arena's edge (next to a tile that is no Banzai tile): an area locked tiles close off.</summary>
    private List<RoomItemStore.PlacedItem>? ClosedOff(RoomItemStore.PlacedItem start)
    {
        var area = new List<RoomItemStore.PlacedItem>();
        var seen = new HashSet<RoomItemStore.PlacedItem> { start };
        var open = new Queue<RoomItemStore.PlacedItem>([start]);
        while (open.TryDequeue(out var tile))
        {
            area.Add(tile);
            foreach (var (dx, dy) in Neighbours)
            {
                if (TileAt(tile.X + dx, tile.Y + dy) is not { } next)
                    return null;
                if (IsLocked(StateOf(next)) || !seen.Add(next))
                    continue;
                open.Enqueue(next);
            }
        }

        return area;
    }

    private static readonly (int Dx, int Dy)[] Neighbours = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    private void Scored(int team, RoomEntityStore.RoomEntity? by, int tiles)
    {
        _pendingPoints[team] = _pendingPoints.GetValueOrDefault(team) + tiles;
        if (by is not null && games.CountsForAchievements)
            actions?.ProgressAchievement(by, AchievementNames.BattleBallTilesLocked, tiles);
    }

    // ---------- the puck ----------

    private void Kick(RoomItemStore.PlacedItem puck, RoomEntityStore.RoomEntity kicker, int dx, int dy)
    {
        if (actions is null || (dx == 0 && dy == 0) || !_rollingPucks.Add(puck.Item.Id))
            return;

        var power = KickPower(puck);
        var team = kicker.Team;
        var at = TimeSpan.Zero;
        for (var step = 1; step <= power; step++)
        {
            var left = power - step;
            at += LastStepTime / (left + 1);
            var last = step == power;
            furniture.Schedule(at, () => RollAsync(puck, dx, dy, left, team, kicker, last));
        }
    }

    /// <summary>One tile of a kick: on, unless something is in the way, which stops it there.</summary>
    private async Task RollAsync(RoomItemStore.PlacedItem puck, int dx, int dy, int left, int team, RoomEntityStore.RoomEntity kicker, bool last)
    {
        if (!_rollingPucks.Contains(puck.Item.Id))
            return;
        if (last)
            _rollingPucks.Remove(puck.Item.Id);
        if (actions is null || !furniture.IsPlaced(puck))
            return;

        var (x, y) = (puck.X + dx, puck.Y + dy);
        // Habbo's puck counts down its power as it rolls: 51, 41, … 11, and stays at 11
        var state = (Math.Max(1, left) * 10 + 1).ToString(CultureInfo.InvariantCulture);
        if (actions.HasAvatarAt(x, y))
        {
            await StopAsync(puck);
            return;
        }

        var before = puck.Data;
        await furniture.SaveAsync(puck, puck.Data with { State = state });
        if (!await actions.MoveItem(puck, x, y, puck.Rotation, false))
        {
            await furniture.SaveAsync(puck, before);
            await StopAsync(puck);
            return;
        }

        if (games.Running && team != RoomGames.NoTeam && TileAt(x, y) is { } tile)
            Colour(tile, team, kicker);
    }

    /// <summary>Something in the way: the puck stops where it is, at rest (11, as Habbo's ends a roll).</summary>
    private async Task StopAsync(RoomItemStore.PlacedItem puck)
    {
        _rollingPucks.Remove(puck.Item.Id);
        if (puck.Data.StateText != RestState)
            await furniture.UpdateAsync(puck, puck.Data with { State = RestState });
    }

    private const string RestState = "11";

    private static int KickPower(RoomItemStore.PlacedItem puck)
    {
        var first = puck.Item.Definition!.CustomParams.Split(',')[0];
        return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var power) && power > 0 ? power : DefaultKickPower;
    }
}
