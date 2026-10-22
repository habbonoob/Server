namespace Habbo.Silo.Items.Interactions;

/// <summary>A pad of notes, and the notes stuck to walls from it. In the inventory the pad's state is
/// how many notes it has left (the catalog product says how many it comes with); on a wall a note's
/// state is its colour, and its text is kept in furni_post_its. The room does the sticking.</summary>
[FurniInteraction(Name)]
public sealed class StickieInteraction : FurniInteraction
{
    public const string Name = "furniture_stickie";
}
