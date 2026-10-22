using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Habbo.Infrastructure;

/// <summary>furnidata in Habbo's JSON form (the hotel's furnidata_json), made from the XML form: the web client
/// reads it with JSON.parse instead of building an XML tree of a million nodes, which froze it for seconds at start
/// on a slow machine. The AIR client keeps reading the XML (it knows no JSON), so both are served, from the one file.
///
/// The shape is the hotel's: { "roomitemtypes": { "furnitype": [ ... ] }, "wallitemtypes": { ... } }, each item
/// an object of its id and classname, then its fields in the XML's order; the counts and ids as numbers, the flags
/// as booleans, partcolors as { "color": [ ... ] }, everything else as the XML's text, so nothing is lost.</summary>
public static class FurnidataJson
{
    private static readonly string[] Sections = ["roomitemtypes", "wallitemtypes"];

    /// <summary>Fields the hotel's JSON gives as whole numbers.</summary>
    private static readonly HashSet<string> Integers =
        ["id", "revision", "defaultdir", "xdim", "ydim", "offerid", "rentofferid", "specialtype", "bcofferid"];

    /// <summary>Fields the hotel's JSON gives as booleans (the XML says 1 or 0).</summary>
    private static readonly HashSet<string> Flags =
        ["buyout", "rentbuyout", "bc", "excludeddynamic", "canstandon", "cansiton", "canlayon", "canputstuffon", "rare", "tradeable", "recyclable"];

    public static string FromXml(string furnidataXml)
    {
        var document = XDocument.Parse(furnidataXml);
        var root = new JsonObject();
        foreach (var name in Sections)
        {
            var items = new JsonArray();
            foreach (var element in document.Root?.Element(name)?.Elements("furnitype") ?? [])
                items.Add(Item(element));
            root[name] = new JsonObject { ["furnitype"] = items };
        }
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonObject Item(XElement element)
    {
        var item = new JsonObject();
        foreach (var attribute in element.Attributes())
            item[attribute.Name.LocalName] = Value(attribute.Name.LocalName, attribute.Value);
        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            item[name] = name == "partcolors"
                ? new JsonObject { ["color"] = new JsonArray(child.Elements("color").Select(c => (JsonNode?)JsonValue.Create(c.Value.Trim())).ToArray()) }
                // element text trimmed, as the client's XML reading has it (E4X with ignoreWhitespace)
                : Value(name, child.Value.Trim());
        }
        return item;
    }

    /// <summary>A field's value as the hotel's JSON types it; the XML's text when it is not what the type expects.</summary>
    private static JsonNode? Value(string name, string text)
    {
        if (Integers.Contains(name) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return JsonValue.Create(number);
        if (Flags.Contains(name) && text is "0" or "1")
            return JsonValue.Create(text == "1");
        return JsonValue.Create(text);
    }
}
