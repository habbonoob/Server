namespace Habbo.Silo.Items.Interactions;

/// <summary>The Battle Banzai puck (the client's furniture_pushable): walking into it kicks it ahead, colouring
/// the tiles it crosses during a game. The room's games run it (<see cref="Rooms.BanzaiArena"/>).</summary>
[FurniInteraction(Name)]
public sealed class BanzaiPuckInteraction : FurniInteraction
{
    public const string Name = "banzai_puck";
}
