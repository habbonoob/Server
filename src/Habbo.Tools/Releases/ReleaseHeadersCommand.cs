using System.Text.RegularExpressions;
using Habbo.Protocol.Releases;

namespace Habbo.Tools.Releases;

/// <summary>The release-headers command: reads a client's message ids from its decompiled source (HabboMessages,
/// and the release ClientHelloMessageComposer reports) into Releases/&lt;release&gt;.json, which the server
/// translates that release's headers by.</summary>
public static partial class ReleaseHeadersCommand
{
    private const string Usage = """
        Habbo.Tools release-headers: reads a client release's message ids from its decompiled source.

          dotnet run --project src/Habbo.Tools -- release-headers <client-src> [--out <dir>]

          <client-src>   the client's AS3 source root (com/sulake/habbo/communication/HabboMessages.as under it)
          --out <dir>    where <release>.json goes (default: src/Habbo.Protocol/Releases)

        Messages the build still hides (_SafeCls_*) are left out: no other release has them by that name.
        """;

    private const string Messages = "com/sulake/habbo/communication/HabboMessages.as";
    private const string Hello = "com/sulake/habbo/communication/messages/outgoing/handshake/ClientHelloMessageComposer.as";

    public static async Task<int> RunAsync(string[] args)
    {
        string? source = null;
        var output = Path.Combine("src", "Habbo.Protocol", "Releases");
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--help" or "-h":
                    Console.WriteLine(Usage);
                    return 0;
                default:
                    if (source is not null || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine(Usage);
                        return 1;
                    }

                    source = args[i];
                    break;
            }
        }

        if (source is null)
        {
            Console.Error.WriteLine(Usage);
            return 1;
        }

        var table = Read(await File.ReadAllTextAsync(Path.Combine(source, Messages)),
            await File.ReadAllTextAsync(Path.Combine(source, Hello)));
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, table.Release + ".json");
        await File.WriteAllTextAsync(path, table.ToJson() + "\n");
        Console.WriteLine($"{path}: {table.Composers.Count} composers, {table.Events.Count} events");
        return 0;
    }

    /// <summary>The table from HabboMessages.as and ClientHelloMessageComposer.as.</summary>
    public static ReleaseHeaders Read(string habboMessages, string clientHello)
    {
        var release = ReleaseLiteral().Match(clientHello);
        if (!release.Success)
            throw new InvalidDataException("no release in ClientHelloMessageComposer");

        return new ReleaseHeaders(release.Groups[1].Value,
            Entries(habboMessages, "_composers"), Entries(habboMessages, "_events"));
    }

    private static SortedDictionary<string, ushort> Entries(string habboMessages, string table)
    {
        var entries = new SortedDictionary<string, ushort>(StringComparer.Ordinal);
        foreach (Match m in Entry().Matches(habboMessages))
        {
            if (m.Groups["table"].Value != table || m.Groups["name"].Value.StartsWith("_Safe", StringComparison.Ordinal))
                continue;
            entries.Add(m.Groups["name"].Value, ushort.Parse(m.Groups["id"].Value));
        }

        return entries;
    }

    [GeneratedRegex(@"(?<table>_composers|_events)\[(?<id>\d+)\]\s*=\s*(?<name>\w+);")]
    private static partial Regex Entry();

    [GeneratedRegex("\"([A-Z0-9]+-\\d{12}-\\d+)\"")]
    private static partial Regex ReleaseLiteral();
}
