using System.Text.Json;
using System.Text.RegularExpressions;

namespace Habbo.Protocol.Tests;

/// <summary>Guards the rule that wire values are never written as bare literals: everything the
/// client receives comes from a parameter, a field, or a named constant that says what it means.
/// This is a machine answer to "did we leave a magic number in a composer?".</summary>
public class NoHardcodedWireValuesTests
{
    /// <summary>Matches the first argument of a MessageWriter call, e.g. w.WriteInt(<c>42</c>, ...).</summary>
    private static readonly Regex WriteCall = new(@"\bw\.Write(\w+)\(\s*([^,()]*?)\s*[,)]", RegexOptions.Compiled);

    /// <summary>A bare literal: a number, a quoted string, a bool, or a char.</summary>
    private static readonly Regex BareLiteral =
        new("""^(?:-?\d+(?:\.\d+)?[fdmFDM]?|".*"|true|false|'.')$""", RegexOptions.Compiled);

    /// <summary>Literals that genuinely belong at the call site, as "file:line-content" fragments.
    /// Add an entry only when the value is meaningless to name — prefer a named constant.</summary>
    private static readonly string[] Allowed = [];

    [Fact]
    public void NoBareLiteralIsWrittenToTheWire()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in WriteCall.Matches(lines[index]))
                {
                    var argument = match.Groups[2].Value;
                    if (!BareLiteral.IsMatch(argument))
                        continue;

                    var offence = $"{RepoRelative(file)}:{index + 1} -> w.Write{match.Groups[1].Value}({argument})";
                    if (!Allowed.Any(offence.Contains))
                        offenders.Add(offence);
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Wire values must come from a parameter, field, or named constant:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void EverySeedFileParses()
    {
        var seed = Path.Combine(RepoRoot(), "seed");
        Assert.True(Directory.Exists(seed), $"Seed directory not found at {seed}");

        var jsonFiles = Directory.GetFiles(seed, "*.json", SearchOption.AllDirectories);
        Assert.NotEmpty(jsonFiles);

        foreach (var file in jsonFiles)
        {
            var exception = Record.Exception(() => JsonDocument.Parse(File.ReadAllText(file)));
            Assert.True(exception is null, $"{RepoRelative(file)} is not valid JSON: {exception?.Message}");
        }
    }

    [Fact]
    public void TheGamedataTheClientDownloadsIsPresent()
    {
        var gamedata = Path.Combine(RepoRoot(), "seed", "gamedata");
        Assert.True(Directory.Exists(gamedata), $"Gamedata directory not found at {gamedata}");

        // The seeder loads exactly these; a missing one means a client that cannot start.
        foreach (var name in new[] { "external_variables", "external_texts", "productdata" })
        {
            var file = Path.Combine(gamedata, $"{name}.txt");
            Assert.True(File.Exists(file), $"Missing seed gamedata file: {RepoRelative(file)}");
        }

        // The client downloads the same furniture list the server imports, so there is only one
        // of them: a client whose furnidata disagrees with ours cannot draw what we send it.
        var furnidata = Path.Combine(RepoRoot(), "seed", "furnidata.xml");
        Assert.True(File.Exists(furnidata), $"Missing seed furnidata: {RepoRelative(furnidata)}");
    }

    /// <summary>The source that builds outgoing messages.</summary>
    private static IEnumerable<string> SourceFiles()
    {
        var root = RepoRoot();
        foreach (var project in new[] { "Habbo.Protocol", "Habbo.Silo", "Habbo.Gateway" })
        {
            var directory = Path.Combine(root, "src", project);
            if (!Directory.Exists(directory))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    yield return file;
            }
        }
    }

    /// <summary>Walks up from the test binary until the solution file turns up.</summary>
    private static string RepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }

    private static string RepoRelative(string path) =>
        Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');
}
