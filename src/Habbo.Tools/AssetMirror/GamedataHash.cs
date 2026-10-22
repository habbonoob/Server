namespace Habbo.Tools.AssetMirror;

/// <summary>One gamedata file: its name, its url, and the hash the url takes after it.</summary>
public sealed record GamedataHash(string Name, string Url, string Hash)
{
    public string FileUrl => $"{Url}/{Hash}";

    /// <summary>What the mirror keeps it as, when not its name with the extension its url says.</summary>
    public string? SaveAs { get; init; }

    /// <summary>The file the mirror keeps it as: its name, with the extension its url says it has (furnidata_xml,
    /// figuredata and the like are xml; the variables and texts are plain text).</summary>
    public string FileName => SaveAs ?? Name + Extension;

    private string Extension
    {
        get
        {
            var last = Url.TrimEnd('/')[(Url.TrimEnd('/').LastIndexOf('/') + 1)..];
            return last.EndsWith("_xml", StringComparison.OrdinalIgnoreCase) || last.Equals("figuredata", StringComparison.OrdinalIgnoreCase) ? ".xml"
                : last.EndsWith("_json", StringComparison.OrdinalIgnoreCase) ? ".json"
                : ".txt";
        }
    }
}
