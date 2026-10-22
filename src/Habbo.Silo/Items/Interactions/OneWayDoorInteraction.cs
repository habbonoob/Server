namespace Habbo.Silo.Items.Interactions;

/// <summary>A one-way door: entered from the side it faces, it lets the avatar through to the other
/// side and closes behind them. Nobody walks through it the other way. The room walks them through.</summary>
[FurniInteraction("furniture_one_way_door")]
public sealed class OneWayDoorInteraction : FurniInteraction
{
    // the door's status, as the client shows it
    public const int Closed = 0;
    public const int Open = 1;
}
