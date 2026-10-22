using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Arena;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;
using Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Ingame;
using Habbo.Silo.Games.SnowWar;
using Habbo.Silo.Games.SnowWar.Engine;
using Habbo.Silo.Games.SnowWar.Engine.Events;
using Orleans;

namespace Habbo.Silo.Grains.Games;

/// <summary>The round, turn by turn. Each turn the game is played one turn on (with the events decided the turn
/// before), then the next turn's events are decided from what the players asked for and what the machines and piles
/// do, and everyone is sent the turn's checksum with them. A client that lost step gets the whole game instead.</summary>
public sealed partial class SnowWarGameGrain
{
    private const int MillisecondsPerSecond = 1000;

    private readonly List<SnowWarAction> _actions = [];
    private readonly HashSet<Guid> _fullStatusFor = [];
    private readonly List<HumanGameObject> _leaving = [];
    private readonly Dictionary<int, int> _lastPickupTurn = [];
    private int _endTurn;

    private static TimeSpan TurnLength => SnowWarArena.SubturnLength * SnowWarArena.SubturnsPerTurn;

    /// <summary>The most a player does in one turn: a move, a snowball made and one thrown, and a little more.</summary>
    private const int MaxActionsPerTurn = 4;

    private Task QueueAsync(SnowWarAction action)
    {
        if (_phase != SnowWarPhase.Running || !_humans.ContainsKey(action.PlayerId) || _gone.Contains(action.PlayerId))
            return Task.CompletedTask;

        // a turn takes a player's last move, and only so many actions: every one goes out to every player
        if (action.Kind == SnowWarActionKind.Move)
            _actions.RemoveAll(a => a.PlayerId == action.PlayerId && a.Kind == SnowWarActionKind.Move);
        if (_actions.Count(a => a.PlayerId == action.PlayerId) < MaxActionsPerTurn)
            _actions.Add(action);
        return Task.CompletedTask;
    }

    private async Task StartRunningAsync()
    {
        if (_phase != SnowWarPhase.Countdown)
            return;

        _timer?.Dispose();
        _phase = SnowWarPhase.Running;
        _endTurn = (int)(TimeSpan.FromSeconds(Rules.GameSeconds) / TurnLength);
        await SendAsync(new Game2StageRunningMessageComposer(Rules.GameSeconds));
        _timer = this.RegisterGrainTimer(async _ => await TickAsync(),
            new GrainTimerCreationOptions { DueTime = TurnLength, Period = TurnLength });
    }

    private async Task TickAsync()
    {
        if (_phase != SnowWarPhase.Running)
            return;

        var turn = _arena.Turn;
        var checksum = _arena.PlayTurn();
        Decide(_arena.Turn);

        var status = StatusToWire(turn, checksum);
        var full = _fullStatusFor.Where(id => !_gone.Contains(id)).ToList();
        _fullStatusFor.Clear();
        var remaining = (int)((_endTurn - turn) * TurnLength.TotalMilliseconds / MillisecondsPerSecond);
        await messenger.SendAsync(Present.Except(full).ToList(), new Game2GameStatusMessageComposer(status));
        if (full.Count > 0)
        {
            await messenger.SendAsync(full, new Game2FullGameStatusMessageComposer(new FullGameStatusData(
                0, remaining, Rules.GameSeconds, ObjectsToWire(), 0, Rules.Teams, status)));
        }

        if (_arena.Turn >= _endTurn || !Present.Any())
            await EndAsync();
    }

    /// <summary>The events of the coming turn, decided on the game as it stands now; all in its first subturn.</summary>
    private void Decide(int turn)
    {
        const int subturn = 0;
        var thrown = new HashSet<int>();

        foreach (var human in _leaving)
            _arena.AddEvent(turn, subturn, new HumanLeftGameEvent(human));
        _leaving.Clear();

        foreach (var action in _actions)
        {
            if (!_humans.TryGetValue(action.PlayerId, out var human) || !human.IsActive || _gone.Contains(action.PlayerId))
                continue;

            switch (action.Kind)
            {
                case SnowWarActionKind.Move:
                    _arena.AddEvent(turn, subturn, new NewMoveTargetEvent(human, action.X, action.Y));
                    break;
                case SnowWarActionKind.MakeSnowball when human.CanMakeSnowballs():
                    _arena.AddEvent(turn, subturn, new HumanStartsToMakeASnowballEvent(human));
                    break;
                case SnowWarActionKind.ThrowAtHuman when human.CanThrowSnowballs() && thrown.Add(human.Id)
                                                         && _stage.GetObject(action.Target) is HumanGameObject { IsActive: true } target
                                                         && target != human && (target.Team != human.Team || _arena.IsDeathMatch):
                    Throw(turn, human, new HumanThrowsSnowballAtHumanEvent(human, target, Trajectory(action)),
                        target.CurrentLocation.X, target.CurrentLocation.Y, Trajectory(action));
                    break;
                case SnowWarActionKind.ThrowAtPosition when human.CanThrowSnowballs() && thrown.Add(human.Id):
                    Throw(turn, human, new HumanThrowsSnowballAtPositionEvent(human, action.X, action.Y, Trajectory(action)),
                        action.X, action.Y, Trajectory(action));
                    break;
            }
        }

        _actions.Clear();

        if (turn % Math.Max(1, Rules.MachineSnowballIntervalTurns) == 0)
        {
            foreach (var machine in _stage.Objects.OfType<SnowballMachineGameObject>().Where(m => m.SnowballCount < m.MaxSnowballs))
                _arena.AddEvent(turn, subturn, new MachineCreatesSnowballEvent(machine));
        }

        foreach (var human in _humans.Values.Where(h => h.IsActive && h.CanMove() && !h.IsMoving && h.RemainingSnowballCapacity > 0))
        {
            if (turn - _lastPickupTurn.GetValueOrDefault(human.Id, int.MinValue / 2) < Rules.PickupIntervalTurns
                || GiverNextTo(human) is not { } giver)
                continue;

            _arena.AddEvent(turn, subturn, new HumanGetsSnowballsFromMachineEvent(human, giver));
            _lastPickupTurn[human.Id] = turn;
        }
    }

    /// <summary>The throw, then the snowball leaving the hand, in the same subturn.</summary>
    private void Throw(int turn, HumanGameObject human, SnowWarEvent throwEvent, int targetX, int targetY, int trajectory)
    {
        _arena.AddEvent(turn, 0, throwEvent);
        _arena.AddEvent(turn, 0, new CreateSnowballEvent(_nextObjectId++, human, targetX, targetY, trajectory));
        _stats.Of(human.Id).SnowballsThrown++;
    }

    /// <summary>A throw as the client knows them; anything else is the default throw, which picks by distance.</summary>
    private static int Trajectory(SnowWarAction action) =>
        action.Trajectory is >= SnowballGameObject.QuickThrow and <= SnowballGameObject.DefaultThrow ? action.Trajectory : SnowballGameObject.DefaultThrow;

    /// <summary>A machine or pile with snowballs on a tile next to the player's.</summary>
    private SnowballGivingGameObject? GiverNextTo(HumanGameObject human)
    {
        for (var direction = 0; direction < 8; direction++)
        {
            if (human.CurrentTile.InDirection(Direction8.Get(direction)!)?.GameObject is SnowballGivingGameObject { SnowballCount: > 0 } giver)
                return giver;
        }

        return null;
    }

    private GameStatusData StatusToWire(int turn, int checksum) => new(turn, checksum,
        _arena.EventsOf(_arena.Turn).Select(events => new SnowWarSubturnData(events.Select(e => e.ToWire()).ToList())).ToList());

    private List<SnowWarGameObjectData> ObjectsToWire() => _stage.Objects.Where(o => o.IsActive).Select(o =>
    {
        var variables = o.Variables().Skip(2).ToList();
        return o is HumanGameObject human
            ? new SnowWarGameObjectData(o.Type, o.Id, variables, human.Name, string.Empty, human.Figure, human.Sex)
            : new SnowWarGameObjectData(o.Type, o.Id, variables);
    }).ToList();
}
