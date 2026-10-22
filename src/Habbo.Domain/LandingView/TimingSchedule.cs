namespace Habbo.Domain.LandingView;

/// <summary>A run of timed codes the landing view asks for by name. The client reads the name
/// out of its own configuration — "landing.view.bgtiming" and the widget slots — sends it up,
/// and draws whichever code comes back, so what is showing when is entirely the hotel's.</summary>
public sealed class TimingSchedule
{
    public int Id { get; set; }

    /// <summary>The name the client sends, echoed back with the answer so a widget can tell
    /// its own reply from another's.</summary>
    public required string SchedulingKey { get; set; }

    public List<TimingScheduleEntry> Entries { get; set; } = [];
}
