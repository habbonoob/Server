using System.Text.RegularExpressions;
using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Addons;

/// <summary>An add-on that names a placeholder the stack's texts may use, written $(name). Its
/// string is the name, and, when it shows several values, a tab and the delimiter between them.</summary>
public abstract partial class WiredPlaceholderAddon : WiredAddon
{
    private const int ShowsMultiple = 0;
    private const char Separator = '\t';
    private const int MaxNameLength = 40;

    public override IReadOnlyList<int> DefaultIntParams => [0];

    protected abstract IEnumerable<string> Values(WiredBox box, WiredRun run);

    public static string Name(WiredBox box) => box.Settings.StringParam.Split(Separator)[0];

    /// <summary>What the placeholder stands for in this run.</summary>
    public string Value(WiredBox box, WiredRun run)
    {
        var values = Values(box, run).ToList();
        if (!box.Flag(ShowsMultiple))
            return values.FirstOrDefault() ?? string.Empty;

        var parts = box.Settings.StringParam.Split(Separator);
        return string.Join(parts.Length > 1 ? parts[1] : string.Empty, values);
    }

    public override WiredValidation? Validate(FurniWired settings)
    {
        var parts = settings.StringParam.Split(Separator);
        if (parts[0].Length is < 1 or > MaxNameLength)
            return WiredValidation.Of("wiredfurni.error.placeholder.name_length");
        if (!NameSyntax().IsMatch(parts[0]))
            return WiredValidation.Of("wiredfurni.error.placeholder.name_syntax");
        if (settings.IntParams.Count > ShowsMultiple && settings.IntParams[ShowsMultiple] != 0
            && (parts.Length < 2 || parts[1].Length == 0))
            return WiredValidation.Of("wiredfurni.error.placeholder.delimiter_empty");
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex NameSyntax();
}
