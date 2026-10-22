using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>The actions wired can see a user perform, by the client's numbers (WiredUserAction):
/// what the perform-action trigger, condition and selector are set to. Sign and dance carry which
/// one as their extra: "3" for sign 3, "dance 2" for the second dance.</summary>
public static class WiredUserActions
{
    public const int Wave = 0;
    public const int Blow = 1;
    public const int Laugh = 2;
    public const int Respect = 3;
    public const int Awake = 4;
    public const int Sleep = 5;
    public const int Sit = 6;
    public const int Stand = 7;
    public const int Lay = 8;
    public const int Sign = 10;
    public const int Dance = 11;

    /// <summary>How long a moment's action (a wave, a laugh) still counts as being performed: the
    /// two seconds the client plays it for.</summary>
    public const int MomentTicks = 4;

    public static string SignExtra(int sign) => sign.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static string DanceExtra(int style) => $"dance {style}";

    /// <summary>Whether the user is performing the action now: holding the posture or the state, or
    /// having done the moment's action just now. An empty extra matches any sign or dance.</summary>
    public static bool IsPerforming(RoomEntityStore.RoomEntity user, int action, string extra, int currentTick) => action switch
    {
        Sleep => user.Asleep,
        Awake => !user.Asleep,
        Sit => user.Sitting,
        Lay => user.Laying,
        Stand => !user.Sitting && !user.Laying,
        Sign => user.Sign is { } sign && (extra.Length == 0 || SignExtra(sign) == extra),
        Dance => user.DanceStyle != 0 && (extra.Length == 0 || DanceExtra(user.DanceStyle) == extra),
        _ => user.LastAction == action && currentTick - user.LastActionTick <= MomentTicks,
    };
}
