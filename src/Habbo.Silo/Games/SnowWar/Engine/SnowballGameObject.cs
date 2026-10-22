namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A snowball in flight: a quick throw, a short lob or a long lob, falling on a parabola until it hits
/// something or the ground. A port of the client's SnowBallGameObject.</summary>
internal sealed class SnowballGameObject(int id) : SnowWarGameObject(id)
{
    public const int TypeId = 1;

    public const int QuickThrow = 0;
    public const int ShortLob = 1;
    public const int LongLob = 2;
    public const int DefaultThrow = 3;

    private const int ThrowVelocity = 2000;
    private const int InitialHeight = 3000;
    private const double LongLobTimeToTargetCoefficient = 0.0007072135785007072;
    private const double ShortLobTimeToTargetCoefficient = 0.000559;
    private const int QuickThrowMaxRange = 20000;
    private const int ShortLobMaxRange = 60000;
    private const int LongLobMaxRange = 100000;
    private const int DefaultThrowToLobCutoffRange = 42000;
    private const int QuickThrowHeightScaling = 10;
    private const int ShortLobHeightScaling = 25;
    private const int LongLobHeightScaling = 50;
    private const int BoundingRadiusValue = 400;

    private readonly Location3D _location = new(0, 0, 0);
    private int _trajectory;
    private int _planarVelocity;
    private int _timeToLive;
    private int _parabolaOffset;

    public Direction360 Direction { get; } = new(0);
    public HumanGameObject ThrowingHuman { get; private set; } = null!;

    public override int Type => TypeId;
    public override int VariableCount => 11;
    public override int BoundingRadius => BoundingRadiusValue;
    public override Location3D Location => _location;

    public override int GetVariable(int index) => index switch
    {
        0 => TypeId,
        1 => Id,
        2 => _location.X,
        3 => _location.Y,
        4 => _location.Z,
        5 => Direction.Value,
        6 => _trajectory,
        7 => _timeToLive,
        8 => ThrowingHuman.Id,
        9 => _parabolaOffset,
        10 => _planarVelocity,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    /// <summary>Thrown from a point towards a target: its direction, how fast and how long it flies.</summary>
    public void Initialize(int x, int y, int z, int trajectory, int targetX, int targetY, HumanGameObject thrower)
    {
        IsActive = true;
        _location.Change(x, y, z);
        var dx = (targetX - x) / 200;
        var dy = (targetY - y) / 200;
        Direction.Set(Direction360.AngleFromComponents(dx, dy));
        var distance = SnowWarMath.FastSqrt(dx * dx + dy * dy) * 200;

        _trajectory = trajectory != DefaultThrow ? trajectory
            : distance <= DefaultThrowToLobCutoffRange ? QuickThrow
            : distance <= ShortLobMaxRange ? ShortLob
            : LongLob;

        if (_trajectory == QuickThrow)
        {
            _timeToLive = QuickThrowMaxRange / ThrowVelocity;
            _planarVelocity = ThrowVelocity;
        }
        else if (_trajectory == ShortLob)
        {
            distance = Math.Min(distance, ShortLobMaxRange);
            _timeToLive = (int)(distance * ShortLobTimeToTargetCoefficient);
            _planarVelocity = _timeToLive == 0 ? 0 : (int)((double)distance / _timeToLive);
        }
        else if (_trajectory == LongLob)
        {
            distance = Math.Min(distance, LongLobMaxRange);
            _timeToLive = (int)(distance * LongLobTimeToTargetCoefficient);
            _planarVelocity = _timeToLive == 0 ? 0 : (int)((double)distance / _timeToLive);
        }

        _parabolaOffset = _timeToLive / 2;
        ThrowingHuman = thrower;
    }

    public override void Subturn(SnowWarStage stage)
    {
        if (!IsActive)
            return;

        _timeToLive--;
        if (_trajectory == QuickThrow)
            UpdatePosition(QuickThrowHeightScaling, capHeight: true);
        else if (_trajectory == ShortLob)
            UpdatePosition(ShortLobHeightScaling, capHeight: false);
        else
            UpdatePosition(LongLobHeightScaling, capHeight: false);

        var tile = stage.TileAt(Tile.ToTileX(_location.X), Tile.ToTileY(_location.Y));
        var landed = TestCollisions(stage, tile) || stage.CollidesWithGround(this);
        if (landed)
            stage.PutOnDeleteList(this);
    }

    private bool TestCollisions(SnowWarStage stage, Tile? tile)
    {
        if (tile is null)
            return false;

        var direction = Direction.ToDirection8();
        return TestCollision(stage, tile)
               || TestCollision(stage, tile.InDirection(direction))
               || TestCollision(stage, tile.InDirection(direction.Rotate45(false)))
               || TestCollision(stage, tile.InDirection(direction.Rotate45(true)));
    }

    private bool TestCollision(SnowWarStage stage, Tile? tile)
    {
        if (tile?.GameObject is not { } target || !target.TestSnowballCollision(this))
            return false;

        target.OnSnowballHit(stage, this);
        return true;
    }

    private void UpdatePosition(int heightScaling, bool capHeight)
    {
        var x = _location.X + Direction.BaseVectorX * _planarVelocity / 255;
        var y = _location.Y + Direction.BaseVectorY * _planarVelocity / 255;
        var fromTop = _timeToLive - _parabolaOffset;
        var z = (_parabolaOffset * _parabolaOffset - fromTop * fromTop) * heightScaling + InitialHeight;
        if (capHeight)
            z = Math.Min(z, InitialHeight);
        _location.Change(x, y, z);
    }
}
