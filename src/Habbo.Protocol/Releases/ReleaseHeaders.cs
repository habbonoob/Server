using System.Reflection;
using System.Text.Json;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Releases;

/// <summary>One client release's message ids, as its own HabboMessages table has them: the client's class names
/// (composers it sends, events it receives) and their headers. Every release renumbers its messages, so a message
/// is the same message in two releases when the client calls it by the same name. Written by
/// <c>Habbo.Tools release-headers</c>; the tables are embedded from Releases/*.json.</summary>
public sealed record ReleaseHeaders(
    string Release,
    IReadOnlyDictionary<string, ushort> Composers,
    IReadOnlyDictionary<string, ushort> Events)
{
    /// <summary>The release whose ids ClientMessages and ServerMessages hold: the server speaks it, and any other
    /// release is translated to it and back.</summary>
    public const string Native = ClientReleases.March2026;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<IReadOnlyDictionary<string, ReleaseHeaders>> Embedded = new(LoadEmbedded);

    /// <summary>Every release the server has a table for, by release.</summary>
    public static IReadOnlyDictionary<string, ReleaseHeaders> All => Embedded.Value;

    public static ReleaseHeaders? Find(string release) => All.GetValueOrDefault(release);

    public static ReleaseHeaders Parse(string json)
        => JsonSerializer.Deserialize<ReleaseHeaders>(json, Json)
           ?? throw new InvalidDataException("empty release headers");

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions(Json) { WriteIndented = true });

    private static IReadOnlyDictionary<string, ReleaseHeaders> LoadEmbedded()
    {
        var assembly = typeof(ReleaseHeaders).Assembly;
        var tables = new Dictionary<string, ReleaseHeaders>(StringComparer.Ordinal);
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("Releases/", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var table = Parse(reader.ReadToEnd());
            tables.Add(table.Release, table);
        }

        return tables;
    }
}
