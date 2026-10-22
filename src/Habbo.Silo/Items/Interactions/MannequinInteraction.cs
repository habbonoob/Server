namespace Habbo.Silo.Items.Interactions;

/// <summary>A mannequin: shows an outfit, with its gender and a name, for visitors to try on. Its data
/// is a map of the three, which those with rights set.</summary>
[FurniInteraction("furniture_mannequin")]
public sealed class MannequinInteraction : FurniInteraction
{
    // the keys the client's mannequin reads
    public const string GenderKey = "GENDER";
    public const string FigureKey = "FIGURE";
    public const string NameKey = "OUTFIT_NAME";
}
