using Habbo.Silo.Games.SnowWar.Engine;
using Habbo.Silo.Games.SnowWar.Engine.Events;

namespace Habbo.Silo.Tests;

public sealed class SnowWarEngineTests
{
    private sealed class NoRules : ISnowWarRules
    {
        public int Hits { get; private set; }
        public int KnockDowns { get; private set; }

        public void SnowballMade(HumanGameObject human)
        {
        }

        public void Hit(HumanGameObject victim, HumanGameObject thrower) => Hits++;

        public void KnockedDown(HumanGameObject victim, HumanGameObject thrower) => KnockDowns++;
    }

    private static (SnowWarArena Arena, SnowWarStage Stage) Field(int size = 12)
    {
        var arena = new SnowWarArena(2);
        var rows = Enumerable.Range(0, size).Select(_ => new string('0', size));
        var stage = new SnowWarStage(arena, size, size, string.Join('\r', rows), []);
        arena.Stage = stage;
        return (arena, stage);
    }

    private static HumanGameObject Human(SnowWarStage stage, int id, int team, int x, int y, ISnowWarRules rules)
    {
        var human = new HumanGameObject(id, id, team, "p" + id, "hd-180-1", "M", stage.TileAt(x, y)!, Direction8.S, rules);
        stage.AddObject(human);
        return human;
    }

    [Fact]
    public void A_player_walks_tile_by_tile_to_where_they_were_sent()
    {
        var (arena, stage) = Field();
        var human = Human(stage, 1, 1, 2, 2, new NoRules());
        arena.AddEvent(0, 0, new NewMoveTargetEvent(human, 6 * Tile.Width, 2 * Tile.Width));

        for (var turn = 0; turn < 20; turn++)
            arena.PlayTurn();

        Assert.Equal(6, human.CurrentTile.X);
        Assert.Equal(2, human.CurrentTile.Y);
        Assert.Equal(6 * Tile.Width, human.CurrentLocation.X);
        Assert.False(human.IsMoving);
    }

    [Fact]
    public void Five_hits_knock_an_opponent_down_and_score_for_the_thrower()
    {
        var (arena, stage) = Field();
        var rules = new NoRules();
        var thrower = Human(stage, 1, 1, 2, 5, rules);
        var target = Human(stage, 2, 2, 5, 5, rules);
        var nextId = 3;

        for (var throwNumber = 0; throwNumber < 5; throwNumber++)
        {
            var turn = arena.Turn;
            arena.AddEvent(turn, 0, new HumanThrowsSnowballAtHumanEvent(thrower, target, SnowballGameObject.QuickThrow));
            arena.AddEvent(turn, 0, new CreateSnowballEvent(nextId++, thrower, target.CurrentLocation.X, target.CurrentLocation.Y,
                SnowballGameObject.QuickThrow));
            thrower.AddSnowballs(1);
            for (var i = 0; i < 10; i++)
                arena.PlayTurn();
        }

        Assert.Equal(5, rules.Hits);
        Assert.Equal(1, rules.KnockDowns);
        Assert.Equal(HumanGameObject.ActivityStunned, target.ActivityState);
        Assert.Equal(4 * HumanGameObject.ScoreOnHit + HumanGameObject.ScoreOnHit + HumanGameObject.ScoreOnKnockDown, thrower.Score);
        Assert.Equal(thrower.Score, arena.TeamScores[0]);
    }

    [Fact]
    public void The_same_game_gives_the_same_checksums()
    {
        static List<int> Play()
        {
            var (arena, stage) = Field();
            var human = Human(stage, 1, 1, 1, 1, new NoRules());
            arena.AddEvent(0, 1, new NewMoveTargetEvent(human, 9 * Tile.Width, 7 * Tile.Width));
            return Enumerable.Range(0, 30).Select(_ => arena.PlayTurn()).ToList();
        }

        Assert.Equal(Play(), Play());
    }

    [Fact]
    public void The_fast_root_is_close_to_the_real_one()
    {
        foreach (var value in new[] { 9, 100, 5000, 123456, 99999999 })
            Assert.InRange(SnowWarMath.FastSqrt(value), Math.Sqrt(value) * 0.95, Math.Sqrt(value) * 1.05);
    }

    [Fact]
    public void Angles_come_out_the_way_the_client_reckons_them()
    {
        Assert.Equal(90, Direction360.AngleFromComponents(10, 0));
        Assert.Equal(180, Direction360.AngleFromComponents(0, 10));
        Assert.Equal(Direction8.SE, Direction360.ToDirection8(135));
        Assert.Equal(Direction8.N, Direction360.ToDirection8(0));
    }
}
