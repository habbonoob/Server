namespace Habbo.Tools.AssetMirror;

/// <summary>A gamedata file the hashes manifest leaves out but the hotel serves beside one it lists, under the same
/// hash: the json forms of furnidata and productdata, and the override texts and variables the client loads over the
/// usual ones.</summary>
public sealed record GamedataExtra(string Path, string BaseName, string FileName)
{
    public static readonly IReadOnlyList<GamedataExtra> All =
    [
        new("furnidata_json", "furnidata", "furnidata.json"),
        new("productdata_json", "productdata", "productdata.json"),
        new("override/external_flash_override_texts", "external_texts", "external_override_texts.txt"),
        new("override/external_override_variables", "external_variables", "external_override_variables.txt"),
    ];
}
