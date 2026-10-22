using System.Globalization;

namespace Habbo.Silo.Rooms;

/// <summary>Freeze's rules, as Habbo writes them in a Freeze tile's custom params
/// ("12,12,12,12:49,50,51,52:10,-5:100:3"): the effect a frozen player wears and the one a player protected after
/// thawing wears, by team; the points a hit is worth and those a hit on a teammate costs the thrower's team; the time
/// a blast takes per tile; the lives a player starts with.</summary>
public sealed record FreezeRules(
    IReadOnlyList<int> FrozenEffects,
    IReadOnlyList<int> ProtectionEffects,
    int HitPoints,
    int TeammateHitPoints,
    TimeSpan BlastStepTime,
    int Lives)
{
    public static readonly FreezeRules Habbo = new([12, 12, 12, 12], [49, 50, 51, 52], 10, -5, TimeSpan.FromMilliseconds(100), 3);

    /// <summary>The rules a tile's custom params give; any part missing or unreadable keeps Habbo's.</summary>
    public static FreezeRules Parse(string customParams)
    {
        var parts = customParams.Split(':');
        var frozen = Ints(parts, 0);
        var protection = Ints(parts, 1);
        var points = Ints(parts, 2);
        var step = Ints(parts, 3);
        var lives = Ints(parts, 4);
        return new FreezeRules(
            frozen is { Count: 4 } ? frozen : Habbo.FrozenEffects,
            protection is { Count: 4 } ? protection : Habbo.ProtectionEffects,
            points is { Count: 2 } ? points[0] : Habbo.HitPoints,
            points is { Count: 2 } ? points[1] : Habbo.TeammateHitPoints,
            step is [> 0] ? TimeSpan.FromMilliseconds(step[0]) : Habbo.BlastStepTime,
            lives is [> 0] ? lives[0] : Habbo.Lives);
    }

    /// <summary>The effect worn, by team (1–4), from a list by team.</summary>
    public static int ForTeam(IReadOnlyList<int> effects, int team) =>
        team >= RoomGames.FirstTeam && team - RoomGames.FirstTeam < effects.Count ? effects[team - RoomGames.FirstTeam] : 0;

    private static List<int>? Ints(string[] parts, int index)
    {
        if (index >= parts.Length)
            return null;

        var values = new List<int>();
        foreach (var part in parts[index].Split(','))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return null;
            values.Add(value);
        }

        return values;
    }
}
