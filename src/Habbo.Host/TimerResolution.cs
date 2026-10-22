using System.Runtime.InteropServices;

namespace Habbo.Host;

/// <summary>
/// Timers to the millisecond on Windows. Its timers fire on a 15.6 ms grid unless a process asks for finer, so a room's
/// 500 ms tick came 20 ms early or late at random, where Habbo's lands within a millisecond (measured from both
/// servers' walks). Since Windows 10 2004 the request holds for this process only. Elsewhere timers are already fine.
/// </summary>
internal static class TimerResolution
{
    private const uint OneMillisecond = 1;

    public static void UseMilliseconds()
    {
        if (OperatingSystem.IsWindows())
            _ = TimeBeginPeriod(OneMillisecond);
    }

    // a plain uint in and out: nothing to marshal, so no source-generated stub (which would need unsafe code)
#pragma warning disable SYSLIB1054
    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint period);
#pragma warning restore SYSLIB1054
}
