namespace Habbo.Tools.AssetMirror;

/// <summary>The mirror command: reads its options, then mirrors until done or stopped with Ctrl+C.</summary>
public static class MirrorCommand
{
    private const string Usage = """
        Habbo.Tools mirror: mirrors a Habbo hotel's gamedata and the assets it names.

          dotnet run --project src/Habbo.Tools -- mirror [options]

          --out <dir>        where the mirror goes (default: habbo-assets)
          --hotel <tld>      com, com.br, de, es, fi, fr, it, nl, com.tr (default: com)
          --only <kinds>     comma separated, of gamedata, furni, figure, effects, pets, badges, images,
                             habbopages, catalogue, badgeparts, habbicons, wanted, sounds
                             (default: all but sounds)
                             libraries (furni, figure, effects, pets) are fetched as .hab, the web
                             client's form: the .swf is not
          --workers <n>      downloads at once, all kinds together (default: 16)
          --limit <n>        at most so many files of each kind, for a quick try
          --max-sample <n>   the highest sound sample number to try (default: 2000)
          --max-icon <n>     the highest catalogue icon number to try (default: 500)
          --badge-parts <f>  the group badge parts to fetch, as the hotel's seed lists them
                             (default: seed/group_badge_parts.json)
          --pages <names>    more help pages to start the habbopages crawl from, comma separated
                             (the ones the client links to itself are always included)
          --force            fetch again what is already there
        """;

    /// <summary>The help pages the client links to itself, which no gamedata names.</summary>
    private static readonly IReadOnlyList<string> ClientPages = ["builders-club/faq", "camera", "chat/commands", "habboclub", "hcpayday"];

    public static async Task<int> RunAsync(string[] args)
    {
        var options = new MirrorOptions("habbo-assets", "com",
            Enum.GetValues<AssetKind>().Where(k => k != AssetKind.Sounds).ToHashSet(), 16, 0, 2000, 500,
            Path.Combine("seed", "group_badge_parts.json"), ClientPages, false);

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");

            try
            {
                options = args[i] switch
                {
                    "--out" => options with { Out = Next() },
                    "--hotel" => options with { Hotel = Next() },
                    "--only" => options with
                    {
                        Kinds = Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(k => Enum.Parse<AssetKind>(k, ignoreCase: true)).ToHashSet(),
                    },
                    "--workers" => options with { Workers = int.Parse(Next()) },
                    "--limit" => options with { Limit = int.Parse(Next()) },
                    "--max-sample" => options with { MaxSample = int.Parse(Next()) },
                    "--max-icon" => options with { MaxIcon = int.Parse(Next()) },
                    "--badge-parts" => options with { BadgePartsFile = Next() },
                    "--pages" => options with
                    {
                        Pages = [.. options.Pages, .. Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
                    },
                    "--force" => options with { Force = true },
                    "--help" or "-h" => throw new ArgumentException(string.Empty),
                    _ => throw new ArgumentException($"unknown option {args[i]}"),
                };
            }
            catch (Exception error) when (error is ArgumentException or FormatException)
            {
                if (error.Message.Length > 0)
                    Console.Error.WriteLine(error.Message);
                Console.Error.WriteLine(Usage);
                return 1;
            }
        }

        options = options with { Out = Path.GetFullPath(options.Out) };
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        using var mirror = new AssetMirror(options);
        try
        {
            await mirror.RunAsync(cancel.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("stopped; run again to carry on where it left off");
            return 2;
        }
    }
}
