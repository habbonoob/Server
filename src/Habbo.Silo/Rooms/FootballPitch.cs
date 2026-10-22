using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.Room.Action;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Rooms;

/// <summary>Football in one room, as measured on Habbo (docs/games.md).
///
/// A walk that ends on the ball kicks it ahead as far as its custom params' first value (6), slowing down; one that
/// goes on over the ball dribbles it a tile ahead, and one whose dribble puts the ball where the walk ends leaves
/// the player standing on it. Walking off the ball one stands on heels it back, as far as a kick. A ball that meets a
/// wall, furniture or an avatar turns back, a tile shorter. One that rolls into a goal scores: the goal lights up,
/// the scoreboards of its team count a goal, the kicker waves and the achievements count; the ball comes back out a
/// tile and stops. A goal only takes a ball from its open side; one that meets its back or side backs off a tile.
///
/// A rolling ball's state counts its power down, 11 a tile left (55, 44, … 11, and 11 at rest), and it faces
/// the way it rolls.</summary>
public sealed class FootballPitch(RoomItemStore items, RoomEntityStore entities, IFurniRoom furniture, GameRoomActions? actions)
{
    private const int DefaultPower = 6;

    /// <summary>How long the ball takes over its last tile; each tile before goes faster, by the power left.</summary>
    private static readonly TimeSpan LastStepTime = TimeSpan.FromMilliseconds(500);

    /// <summary>How long a goal stays lit.</summary>
    private static readonly TimeSpan GoalLitTime = TimeSpan.FromSeconds(2);

    private const string Rest = "11";

    /// <summary>Who dribbled each ball last, by ball: their walk ending on it does not kick it.</summary>
    private readonly Dictionary<int, RoomEntityStore.RoomEntity> _dribbledBy = [];

    /// <summary>Each rolling ball's roll, so a new kick or the end of one stops an older one.</summary>
    private readonly Dictionary<int, int> _rolls = [];

    public static bool IsBall(RoomItemStore.PlacedItem item) => item.Interaction is FootballInteraction;

    private RoomItemStore.PlacedItem? BallAt(int x, int y) => items.FloorItemsOn(x, y).FirstOrDefault(IsBall);

    private RoomItemStore.PlacedItem? GoalAt(int x, int y) =>
        items.Items.FirstOrDefault(item => item.Interaction is FootballGoalInteraction && RoomItemStore.Footprint(item).Contains((x, y)));

    /// <summary>A user stepped onto an item, from a tile; <paramref name="arriving"/> when the walk ends there.</summary>
    public async Task SteppedOnAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item, int fromX, int fromY, bool arriving)
    {
        if (!IsBall(item) || actions is null || _rolls.ContainsKey(item.Item.Id))
            return;

        var (dx, dy) = (Math.Sign(item.X - fromX), Math.Sign(item.Y - fromY));
        if (dx == 0 && dy == 0)
            return;

        var dribbled = _dribbledBy.TryGetValue(item.Item.Id, out var by) && by == user;
        if (arriving && dribbled)
            return;
        if (arriving)
            Kick(item, user, dx, dy);
        else
            await DribbleAsync(item, user, dx, dy);
    }

    /// <summary>A user walked off a tile: a ball they stood on is heeled back the other way.</summary>
    public void SteppedOff(RoomEntityStore.RoomEntity user, int fromX, int fromY)
    {
        if (actions is null || BallAt(fromX, fromY) is not { } ball || _rolls.ContainsKey(ball.Item.Id))
            return;

        var (dx, dy) = (Math.Sign(user.X - fromX), Math.Sign(user.Y - fromY));
        if (dx != 0 || dy != 0)
            Kick(ball, user, -dx, -dy);
    }

    private async Task DribbleAsync(RoomItemStore.PlacedItem ball, RoomEntityStore.RoomEntity user, int dx, int dy)
    {
        var (x, y) = (ball.X + dx, ball.Y + dy);
        if (actions!.HasAvatarAt(x, y) || GoalAt(x, y) is not null)
            return;
        if (await MoveAsync(ball, x, y, dx, dy, Rest, intoGoal: false))
            _dribbledBy[ball.Item.Id] = user;
    }

    private void Kick(RoomItemStore.PlacedItem ball, RoomEntityStore.RoomEntity kicker, int dx, int dy)
    {
        _dribbledBy.Remove(ball.Item.Id);
        var roll = _rolls.GetValueOrDefault(ball.Item.Id) + 1;
        _rolls[ball.Item.Id] = roll;
        var power = Power(ball);
        furniture.Schedule(LastStepTime / power, () => RollAsync(ball, kicker, dx, dy, power - 1, roll, bounced: false));
    }

    /// <summary>One tile of a roll, <paramref name="left"/> more after it: on, into a goal, or back off what is in the
    /// way.</summary>
    private async Task RollAsync(RoomItemStore.PlacedItem ball, RoomEntityStore.RoomEntity kicker, int dx, int dy, int left, int roll, bool bounced)
    {
        if (_rolls.GetValueOrDefault(ball.Item.Id) != roll || !furniture.IsPlaced(ball))
            return;

        var (x, y) = (ball.X + dx, ball.Y + dy);
        if (GoalAt(x, y) is { } shut && !Scores(shut, dx, dy))
        {
            // the goal's back or side: back off it a tile, at half the power it came with, and still (Habbo: a goal
            // at dir 0 hit from the south at 44 sent the ball back a tile at 22)
            _rolls.Remove(ball.Item.Id);
            await MoveAsync(ball, ball.X - dx, ball.Y - dy, -dx, -dy, State((left + 1) / 2), intoGoal: false);
            return;
        }

        if (GoalAt(x, y) is { } goal)
        {
            await MoveAsync(ball, x, y, dx, dy, State(left), intoGoal: true);
            await ScoreAsync(goal, kicker);
            // out of the net, a tile back, at half the power it went in with, and still
            furniture.Schedule(LastStepTime / Math.Max(1, left), async () =>
            {
                if (_rolls.GetValueOrDefault(ball.Item.Id) != roll)
                    return;
                _rolls.Remove(ball.Item.Id);
                await MoveAsync(ball, ball.X - dx, ball.Y - dy, -dx, -dy, State(left / 2), intoGoal: true);
            });
            return;
        }

        var moved = !actions!.HasAvatarAt(x, y) && await MoveAsync(ball, x, y, dx, dy, bounced ? Rest : State(left), intoGoal: false);
        if (!moved)
        {
            // something in the way: back the other way, a tile shorter, slowly (Habbo: off a wall at 33, then 11, 11)
            if (left <= 0)
            {
                _rolls.Remove(ball.Item.Id);
                return;
            }

            (dx, dy, bounced) = (-dx, -dy, true);
            (x, y) = (ball.X + dx, ball.Y + dy);
            left--;
            if (actions.HasAvatarAt(x, y) || !await MoveAsync(ball, x, y, dx, dy, Rest, intoGoal: false))
            {
                _rolls.Remove(ball.Item.Id);
                return;
            }
        }

        if (left <= 0)
        {
            _rolls.Remove(ball.Item.Id);
            return;
        }

        // each tile takes longer as the power runs down: 0.10, 0.125, 0.17, 0.25, 0.5 s (Habbo)
        furniture.Schedule(LastStepTime / left, () => RollAsync(ball, kicker, dx, dy, left - 1, roll, bounced));
    }

    /// <summary>Whether a ball rolling this way goes into the goal: only from its open side. Habbo: a goal at dir 0
    /// took a ball rolling south, one at dir 4 a ball rolling north, so a goal takes what rolls the way its direction
    /// turned around points.</summary>
    private static bool Scores(RoomItemStore.PlacedItem goal, int dx, int dy)
    {
        var (ix, iy) = Vector((goal.Rotation + 4) % 8);
        return dx * ix + dy * iy > 0;
    }

    private static (int Dx, int Dy) Vector(int rotation) => (rotation % 8) switch
    {
        0 => (0, -1),
        1 => (1, -1),
        2 => (1, 0),
        3 => (1, 1),
        4 => (0, 1),
        5 => (-1, 1),
        6 => (-1, 0),
        _ => (-1, -1),
    };

    private static string State(int left) => (Math.Max(1, left) * 11).ToString(CultureInfo.InvariantCulture);

    /// <summary>The ball onto a tile, facing the way it goes, in a state of its roll.</summary>
    private async Task<bool> MoveAsync(RoomItemStore.PlacedItem ball, int x, int y, int dx, int dy, string state, bool intoGoal)
    {
        var before = ball.Data;
        await furniture.SaveAsync(ball, ball.Data with { State = state });
        if (await actions!.MoveItem(ball, x, y, Facing(dx, dy), intoGoal))
            return true;

        await furniture.SaveAsync(ball, before);
        return false;
    }

    /// <summary>A ball in a goal: it lights up, its team's scoreboards count a goal, the kicker waves.</summary>
    private async Task ScoreAsync(RoomItemStore.PlacedItem goal, RoomEntityStore.RoomEntity kicker)
    {
        await furniture.UpdateAsync(goal, goal.Data with { State = "1" }, persist: false);
        furniture.Schedule(GoalLitTime, () => furniture.IsPlaced(goal) ? furniture.UpdateAsync(goal, goal.Data with { State = "0" }, persist: false) : Task.CompletedTask);

        var team = int.TryParse(goal.Item.Definition!.CustomParams, NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) ? t : 0;
        foreach (var board in items.Items.Where(item => item.Interaction is HockeyScoreInteraction && HockeyScoreInteraction.Team(item) == team).ToList())
        {
            var score = Math.Min(HockeyScoreInteraction.Top(board), HockeyScoreInteraction.Score(board) + 1);
            await furniture.UpdateAsync(board, board.Data with { State = score.ToString(CultureInfo.InvariantCulture) });
        }

        if (entities.Find(kicker.Player.Id) == kicker)
        {
            furniture.Broadcast(new ExpressionMessageComposer(kicker.RoomIndex, (int)AvatarExpression.Wave));
            actions!.ProgressAchievement(kicker, AchievementNames.FootballGoalScored, 1);
        }

        actions!.ProgressOwnerAchievement?.Invoke(AchievementNames.FootballGoalScoredInRoom, 1);
    }

    /// <summary>The rotation facing a direction: 0 is up the y axis, counting clockwise in eighths.</summary>
    private static int Facing(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 0,
        (1, -1) => 1,
        (1, 0) => 2,
        (1, 1) => 3,
        (0, 1) => 4,
        (-1, 1) => 5,
        (-1, 0) => 6,
        _ => 7,
    };

    private static int Power(RoomItemStore.PlacedItem ball) =>
        int.TryParse(ball.Item.Definition!.CustomParams.Split(',')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var power) && power > 0
            ? power
            : DefaultPower;
}
