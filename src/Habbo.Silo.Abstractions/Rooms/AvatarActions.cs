namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>What an avatar can express (the client's AvatarExpressionEnum).</summary>
public enum AvatarExpression
{
    None = 0,
    Wave = 1,
    Blow = 2,
    Laugh = 3,
    Cry = 4,

    /// <summary>":idle": falls asleep rather than animating.</summary>
    Idle = 5,
    Jump = 6,
    Respect = 7,

    /// <summary>The club's "67": the expression, and a special system chat line of the same number over the avatar.</summary>
    Expression67 = 67,
}

public static class AvatarActions
{
    /// <summary>Stops dancing.</summary>
    public const int NoDance = 0;

    /// <summary>The dance everyone has; the others are the club's.</summary>
    public const int FreeDance = 1;

    public const int LastDance = 4;

    public const int FirstSign = 0;
    public const int LastSign = 17;
}
