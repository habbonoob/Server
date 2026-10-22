using System.Globalization;
using System.Xml.Linq;
using Habbo.Domain.Furniture;

namespace Habbo.Infrastructure;

/// <summary>Reads Habbo's furnidata into item definitions. furnidata is the client's own view
/// of what furniture exists, so it is the authority for dimensions, heights and what an avatar
/// may do with an item — anything we invent here would look right and behave wrong.
///
/// The file is two sections: roomitemtypes are floor items, wallitemtypes are wall items.
/// Wall items carry no geometry at all, so they take the defaults.</summary>
public static class FurnidataParser
{
    private const string FloorSection = "roomitemtypes";
    private const string WallSection = "wallitemtypes";

    /// <summary>Every definition in the document, floor items first. Malformed entries are
    /// skipped rather than thrown on: furnidata is a live export and one bad row should not
    /// cost the other eighteen thousand.</summary>
    public static IEnumerable<ItemDefinition> Parse(Stream furnidata)
    {
        var document = XDocument.Load(furnidata);

        foreach (var definition in Section(document, FloorSection, ItemType.Floor))
            yield return definition;

        foreach (var definition in Section(document, WallSection, ItemType.Wall))
            yield return definition;
    }

    private static IEnumerable<ItemDefinition> Section(XDocument document, string name, ItemType type)
    {
        var section = document.Root?.Element(name);
        if (section is null)
            yield break;

        foreach (var element in section.Elements("furnitype"))
        {
            var definition = ToDefinition(element, type);
            if (definition is not null)
                yield return definition;
        }
    }

    /// <summary>Null when the entry has no usable id or class name, which are the two things
    /// everything else is keyed on.</summary>
    private static ItemDefinition? ToDefinition(XElement element, ItemType type)
    {
        if (!int.TryParse((string?)element.Attribute("id"), out var id))
            return null;

        var className = (string?)element.Attribute("classname");
        if (string.IsNullOrWhiteSpace(className))
            return null;

        return new ItemDefinition
        {
            // Id is ours and generated on insert; the furnidata number is the class id.
            ClassId = id,
            ClassName = className,
            Type = type,
            XDim = Int(element, "xdim", 1),
            YDim = Int(element, "ydim", 1),
            StackHeight = Double(element, "height", 1.0),
            CanWalk = Flag(element, "canstandon"),
            CanSit = Flag(element, "cansiton"),
            CanLay = Flag(element, "canlayon"),
            CanStackOn = Flag(element, "canputstuffon"),
            CategoryName = Text(element, "category"),
            Name = Text(element, "name"),
            Description = Text(element, "description"),
            Revision = Int(element, "revision", 0),
            DefaultDirection = Int(element, "defaultdir", 0),
            SpecialType = Int(element, "specialtype", 1),
            FurniLine = Text(element, "furniline"),
            Rare = Flag(element, "rare"),
            Tradeable = Flag(element, "tradeable", whenMissing: true),
            Recyclable = Flag(element, "recyclable", whenMissing: true),
            CustomParams = Text(element, "customparams").Trim(),
        };
    }

    private static string Text(XElement element, string name) =>
        (string?)element.Element(name) ?? string.Empty;

    private static int Int(XElement element, string name, int fallback) =>
        int.TryParse((string?)element.Element(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    /// <summary>Heights are written with a decimal point regardless of the machine's locale,
    /// so they are parsed with the invariant culture rather than the current one.</summary>
    private static double Double(XElement element, string name, double fallback) =>
        double.TryParse((string?)element.Element(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    /// <summary>furnidata writes booleans as 0 or 1.</summary>
    private static bool Flag(XElement element, string name, bool whenMissing = false)
    {
        var text = (string?)element.Element(name);
        return string.IsNullOrWhiteSpace(text) ? whenMissing : text.Trim() == "1";
    }
}
