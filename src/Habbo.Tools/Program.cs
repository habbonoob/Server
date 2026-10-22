using Habbo.Tools.AssetMirror;
using Habbo.Tools.Releases;

// The hotel's tools, one command each: dotnet run --project src/Habbo.Tools -- <command> [options]

const string Usage = """
    Habbo.Tools <command> [options]

      mirror            mirrors a Habbo hotel's gamedata and the assets it names (mirror --help)
      release-headers   reads a client release's message ids from its decompiled source (release-headers --help)
    """;

if (args.Length == 0)
{
    Console.Error.WriteLine(Usage);
    return 1;
}

var rest = args[1..];
return args[0] switch
{
    "mirror" => await MirrorCommand.RunAsync(rest),
    "release-headers" => await ReleaseHeadersCommand.RunAsync(rest),
    _ => Unknown(args[0]),
};

static int Unknown(string command)
{
    Console.Error.WriteLine($"unknown command {command}");
    Console.Error.WriteLine(Usage);
    return 1;
}
