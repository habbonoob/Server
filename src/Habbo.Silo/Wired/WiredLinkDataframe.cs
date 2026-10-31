using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Habbo.Silo.Wired;

/// <summary>A link dataframe box's rows as its dialog saves them (the client's LinkDataframeCodec): the string is a JSON
/// array of [key, text] pairs, one per row in use, and the int parameters start with the row count. Up to 12 rows; a
/// key is a variable-style name of up to 40 characters, matched however it is cased and with spaces as underscores.</summary>
public static partial class WiredLinkDataframe
{
    public const int MaxRows = 12;
    public const int MaxKeyLength = 40;

    /// <summary>The most the rows' string may take, in UTF-8 bytes; and the most the int parameters and variable ids may
    /// take joined with "|" (LinkDataframeCodec.storageError).</summary>
    public const int MaxTextBytes = 3000;
    public const int MaxJoinedIntParams = 400;
    public const int MaxJoinedVariableIds = 240;

    /// <summary>The longest custom message one row may carry, in UTF-8 bytes.</summary>
    public const int MaxMessageBytes = 200;

    /// <summary>The variable id the dialogs save for a row with no variable (the client's WiredVariable "none").</summary>
    public const string NoVariable = "n";

    public const string Invalid = "wiredfurni.error.link.invalid_dataframe";
    public const string TooLarge = "wiredfurni.error.link.configuration_too_large";
    public const string KeyLength = "wiredfurni.error.link.key_length";
    public const string NameSyntax = "wiredfurni.error.variables.name_syntax";
    public const string NameNotUnique = "wiredfurni.error.variables.name_uniq";

    /// <summary>The rows the string holds, when it holds exactly <paramref name="count"/> of them; null when it is not a
    /// dialog's.</summary>
    public static IReadOnlyList<(string Key, string Text)>? Decode(string json, int count)
    {
        if (count is < 0 or > MaxRows)
            return null;
        if (json.Length == 0 && count == 0)
            return [];

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() != count)
                return null;

            var rows = new List<(string, string)>();
            foreach (var row in document.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != 2
                    || row[0].ValueKind != JsonValueKind.String || row[1].ValueKind != JsonValueKind.String)
                    return null;
                rows.Add((row[0].GetString()!, row[1].GetString()!));
            }

            return rows;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A key as the dialogs compare keys: underscores as spaces, trimmed, spaces back to underscores, lowercase.</summary>
    public static string NormalizeKey(string key) =>
        key.Replace('_', ' ').Trim(Controls).Replace(' ', '_').ToLowerInvariant();

    /// <summary>Why a key may not be used; null when it may.</summary>
    public static string? KeyError(string key)
    {
        key = NormalizeKey(key);
        if (key.Length is 0 or > MaxKeyLength)
            return KeyLength;
        return Word().IsMatch(key) ? null : NameSyntax;
    }

    /// <summary>Why the saved parts are too large to keep; null when they fit.</summary>
    public static string? StorageError(string json, IReadOnlyList<int> intParams, IReadOnlyList<string> variableIds) =>
        Utf8Length(json) > MaxTextBytes
        || string.Join('|', intParams.Select(i => i.ToString(CultureInfo.InvariantCulture))).Length > MaxJoinedIntParams
        || string.Join('|', variableIds).Length > MaxJoinedVariableIds
            ? TooLarge
            : null;

    public static int Utf8Length(string text) => Encoding.UTF8.GetByteCount(text);

    /// <summary>The characters the dialogs trim from a key's ends: space and everything below it.</summary>
    private static readonly char[] Controls = Enumerable.Range(0, ' ' + 1).Select(c => (char)c).ToArray();

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex Word();
}
