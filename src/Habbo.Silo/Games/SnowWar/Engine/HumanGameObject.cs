namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A player: walks tile by tile towards where they were told, makes and throws snowballs, is knocked down
/// when hit too often, and gets up invincible for a while. A port of the client's HumanGameObject.</summary>
internal sealed class HumanGameObject : SnowWarGameObject
{
    public const int TypeId = 5;
    public const int MaximumSnowballs = 5;
    public const int InitialSnowballs = 5;
    public const int InitialHitPoints = 5;
    public const int SnowballCreateTime = 20;
    public const int StunTime = 100;
    public const int InvincibleAfterStunTime = 60;
    public const int SnowballThrowInterval = 5;
    public const int PlayerHeight = 5000;
    public const int ScoreOnHit = 1;
    public const int ScoreOnKnockDown = 5;

    public const int ActivityNormal = 0;
    public const int ActivityMakingSnowball = 1;
    public const int ActivityStunned = 2;
    public const int ActivityInvincibleAfterStun = 3;

    /// <summary>How far a step goes each subturn.</summary>
    private const int StepLength = 534;
    private const int BoundingRadiusValue = 1600;

    private Tile _currentTile;
    private Tile? _nextTile;
    private readonly Location3D _location = new(0, 0, 0);
    private readonly Location3D _moveTarget = new(0, 0, 0);
    private Direction8 _bodyDirection;
    private int _throwTimer;
    private readonly ISnowWarRules _rules;

    public HumanGameObject(int id, int userId, int team, string name, string figure, string sex, Tile tile, Direction8 direction, ISnowWarRules rules)
        : base(id)
    {
        UserId = userId;
        Team = team;
        Name = name;
        Figure = figure;
        Sex = sex;
        HitPoints = InitialHitPoints;
        Snowballs = InitialSnowballs;
        _bodyDirection = direction;
        _currentTile = tile;
        _location.Change2D(tile.Location.X, tile.Location.Y);
        _moveTarget.Change2D(tile.Location.X, tile.Location.Y);
        tile.AddGameObject(this);
        _rules = rules;
        IsActive = true;
    }

    public int UserId { get; }
    public int Team { get; }
    public string Name { get; }
    public string Figure { get; }
    public string Sex { get; }
    public int HitPoints { get; private set; }
    public int Snowballs { get; private set; }
    public int ActivityState { get; private set; }
    public int ActivityTimer { get; private set; }
    public int Score { get; private set; }
    public bool IsMoving { get; private set; }

    public Location3D CurrentLocation => _location;
    public Tile CurrentTile => _currentTile;
    public Location3D MoveTarget => _moveTarget;

    public override int Type => TypeId;
    public override int VariableCount => 19;
    public override int BoundingRadius => BoundingRadiusValue;
    public override Location3D Location => _location;
    public override int CollisionHeight => PlayerHeight;

    public override int GetVariable(int index) => index switch
    {
        0 => TypeId,
        1 => Id,
        2 => _location.X,
        3 => _location.Y,
        4 => _currentTile.X,
        5 => _currentTile.Y,
        6 => _bodyDirection.Value,
        7 => HitPoints,
        8 => Snowballs,
        9 => 0, // is a bot: there are none
        10 => ActivityTimer,
        11 => ActivityState,
        12 => (_nextTile ?? _currentTile).X,
        13 => (_nextTile ?? _currentTile).Y,
        14 => _moveTarget.X,
        15 => _moveTarget.Y,
        16 => Score,
        17 => Team,
        18 => UserId,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    public override void OnRemove()
    {
        if (_currentTile.OccupyingHuman == this)
            _currentTile.RemoveOccupyingHuman();
        if (_nextTile?.OccupyingHuman == this)
            _nextTile.RemoveOccupyingHuman();
        IsMoving = false;
    }

    public override void Subturn(SnowWarStage stage)
    {
        if (ActivityTimer > 0)
        {
            if (ActivityTimer == 1)
                ActivityTimerTriggered();
            ActivityTimer--;
        }

        if (_throwTimer > 0)
            _throwTimer--;

        if (!CanMove())
        {
            IsMoving = false;
            return;
        }

        if (_nextTile is not null)
        {
            MoveTowardsNextTile();
            return;
        }

        if (_currentTile.LocationIsInTileRange(_moveTarget))
        {
            IsMoving = false;
            return;
        }

        var angle = Direction360.AngleFromComponents(_moveTarget.X - _currentTile.Location.X, _moveTarget.Y - _currentTile.Location.Y);
        var direction = Direction360.ToDirection8(angle);
        _nextTile = _currentTile.InDirection(direction);
        if (_nextTile is null || !_nextTile.CanMoveTo())
        {
            if (_nextTile is not null && _moveTarget.SameAs(_nextTile.Location))
            {
                _nextTile = null;
                StopMovement();
                return;
            }

            direction = direction.Rotate(-1);
            _nextTile = _currentTile.InDirection(direction);
            if (_nextTile is null || !_nextTile.CanMoveTo())
            {
                direction = direction.Rotate(2);
                _nextTile = _currentTile.InDirection(direction);
                if (_nextTile is not null && !_nextTile.CanMoveTo())
                    _nextTile = null;
            }
        }

        if (_nextTile is not null)
        {
            _currentTile.RemoveOccupyingHuman();
            _nextTile.AddGameObject(this);
            _bodyDirection = direction;
            MoveTowardsNextTile();
        }
        else
        {
            IsMoving = false;
        }
    }

    public override bool TestSnowballCollision(SnowballGameObject snowball) =>
        ActivityState != ActivityStunned && ActivityState != ActivityInvincibleAfterStun
        && snowball.ThrowingHuman != this && base.TestSnowballCollision(snowball);

    public override void OnSnowballHit(SnowWarStage stage, SnowballGameObject snowball)
    {
        var thrower = snowball.ThrowingHuman;
        HitBySnowball(stage, thrower, snowball.Direction.Value);
        thrower.OnHitHuman(stage, this);
    }

    private void ActivityTimerTriggered()
    {
        if (ActivityState == ActivityStunned)
        {
            HitPoints = InitialHitPoints;
            ActivityState = ActivityInvincibleAfterStun;
            ActivityTimer = InvincibleAfterStunTime;
            return;
        }

        if (ActivityState == ActivityMakingSnowball)
        {
            Snowballs++;
            _rules.SnowballMade(this);
        }

        ActivityState = ActivityNormal;
    }

    public void ChangeMoveTarget(int x, int y)
    {
        if (ActivityState == ActivityMakingSnowball)
        {
            ActivityState = ActivityNormal;
            ActivityTimer = 0;
        }

        if (ActivityState is ActivityNormal or ActivityInvincibleAfterStun)
            _moveTarget.Change2D(x, y);
    }

    private void HitBySnowball(SnowWarStage stage, HumanGameObject thrower, int direction)
    {
        // no hurting one's own team, except in a game of one team where everyone is against everyone
        if (thrower == this || (Team == thrower.Team && !stage.Arena.IsDeathMatch) || HitPoints <= 0)
            return;

        if (HitPoints == 1)
        {
            FallDown(direction);
            thrower.OnKnockDownHuman(stage, this);
        }

        HitPoints--;
        _rules.Hit(this, thrower);
    }

    private void OnHitHuman(SnowWarStage stage, HumanGameObject victim)
    {
        if (Team != victim.Team || stage.Arena.IsDeathMatch)
            AddScore(stage.Arena, ScoreOnHit);
    }

    private void OnKnockDownHuman(SnowWarStage stage, HumanGameObject victim)
    {
        if (Team != victim.Team || stage.Arena.IsDeathMatch)
            AddScore(stage.Arena, ScoreOnKnockDown);
        _rules.KnockedDown(victim, this);
    }

    private void AddScore(SnowWarArena arena, int score)
    {
        Score += score;
        arena.AddTeamScore(Team, score);
    }

    private void FallDown(int direction)
    {
        ActivityState = ActivityStunned;
        ActivityTimer = StunTime;
        _bodyDirection = Direction360.ToDirection8(direction).Opposite();
        StopMovement();
    }

    public void StopMovement()
    {
        if (_nextTile is null)
        {
            _moveTarget.ChangeTo(_currentTile.Location);
            _location.ChangeTo(_currentTile.Location);
        }
        else
        {
            _currentTile = _nextTile;
            _location.ChangeTo(_nextTile.Location);
            _moveTarget.ChangeTo(_nextTile.Location);
            _nextTile = null;
        }

        IsMoving = false;
    }

    public bool CanThrowSnowballs() => Snowballs > 0 && _throwTimer < 1 && ActivityState is ActivityNormal or ActivityInvincibleAfterStun;

    public void StartThrowTimer() => _throwTimer = SnowballThrowInterval;

    public bool ThrowSnowball(int x, int y)
    {
        if (Snowballs < 1)
            return false;

        StopMovement();
        var angle = Direction360.AngleFromComponents(x - _location.X, y - _location.Y);
        _bodyDirection = Direction360.ToDirection8(angle);
        Snowballs--;
        return true;
    }

    public bool CanMove() => ActivityState is ActivityNormal or ActivityInvincibleAfterStun;

    public bool CanMakeSnowballs() => ActivityState is ActivityNormal or ActivityInvincibleAfterStun && Snowballs < MaximumSnowballs;

    public void StartMakingSnowball()
    {
        if (!CanMakeSnowballs())
            return;
        ActivityState = ActivityMakingSnowball;
        ActivityTimer = SnowballCreateTime;
        StopMovement();
    }

    public int RemainingSnowballCapacity => MaximumSnowballs - Snowballs;

    public void AddSnowballs(int count) => Snowballs += count;

    /// <summary>Snowballs taken from a machine or a pile: in the hand, and counted for the results.</summary>
    public void TakeFromMachine(int count)
    {
        AddSnowballs(count);
        _rules.SnowballsFromMachine(this, count);
    }

    private void MoveTowardsNextTile()
    {
        var next = _nextTile!;
        _location.Change2D(Step(_location.X, next.Location.X), Step(_location.Y, next.Location.Y));
        if (_location.DistanceTo(next.Location) < StepLength / 2)
        {
            _currentTile = next;
            _nextTile = null;
        }

        IsMoving = true;
    }

    /// <summary>One step along an axis: all the way when closer than a step, a step's length otherwise.</summary>
    private static int Step(int from, int to)
    {
        var difference = from - to;
        if (difference == 0)
            return from;
        if (difference < 0)
            return difference > -StepLength ? to : from + StepLength;
        return difference < StepLength ? to : from - StepLength;
    }
}
