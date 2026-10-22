namespace Habbo.Silo.Users;

/// <summary>Figure strings as outfits: the clothing parts of a look, and a look wearing an outfit.
/// A figure is parts separated by '.', each starting with its set type ("ch-215-62").</summary>
public static class FigureOutfit
{
    private const char PartSeparator = '.';
    private const char FieldSeparator = '-';

    /// <summary>Only the parts of the figure whose set types are clothing.</summary>
    public static string Clothing(string figure, IReadOnlyCollection<string> clothingSetTypes) =>
        string.Join(PartSeparator, Parts(figure).Where(part => clothingSetTypes.Contains(SetType(part))));

    /// <summary>The figure wearing the outfit: the outfit's parts replace those of the same set type.</summary>
    public static string Wear(string figure, string outfit)
    {
        var outfitParts = Parts(outfit).ToList();
        var replaced = outfitParts.Select(SetType).ToHashSet();
        return string.Join(PartSeparator, Parts(figure).Where(part => !replaced.Contains(SetType(part))).Concat(outfitParts));
    }

    private static IEnumerable<string> Parts(string figure) =>
        figure.Split(PartSeparator, StringSplitOptions.RemoveEmptyEntries);

    private static string SetType(string part) => part.Split(FieldSeparator)[0];
}
