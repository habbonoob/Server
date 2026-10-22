namespace Habbo.Silo.Items.Interactions;

/// <summary>The Freeze exit (es_exit): lit (1) while a game runs, and where a player out of lives is sent.
/// Multistate to the client, so without this a double-click would switch it. The room's games run it
/// (<see cref="Rooms.FreezeArena"/>).</summary>
[FurniInteraction(Name)]
public sealed class FreezeExitInteraction : FurniInteraction
{
    public const string Name = "freeze_exit";
}
