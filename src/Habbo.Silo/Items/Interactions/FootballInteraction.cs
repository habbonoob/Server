namespace Habbo.Silo.Items.Interactions;

/// <summary>A football (fball_ball*, the client's furniture_pushable): walked into it is kicked, walked over it is
/// dribbled, stood on and walked off it is heeled back. The room's games run it (<see cref="Rooms.FootballPitch"/>).</summary>
[FurniInteraction(Name)]
public sealed class FootballInteraction : FurniInteraction
{
    public const string Name = "football";
}
