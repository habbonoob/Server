namespace Habbo.Silo.Items.Interactions;

/// <summary>A Freeze tile (es_tile, the client's furniture_es): a player double-clicks it to drop a snowball on
/// the tile they stand on. Its custom params hold the game's rules (<see cref="Rooms.FreezeRules"/>); the room's
/// games run it (<see cref="Rooms.FreezeArena"/>).</summary>
[FurniInteraction(Name)]
public sealed class FreezeTileInteraction : FurniInteraction
{
    public const string Name = "freeze_tile";
}
