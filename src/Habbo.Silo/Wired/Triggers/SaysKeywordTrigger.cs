using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Addons;

namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a user says the box's text. Its dialog: whether only the room owner counts, how the text must
/// match, and whether to hide what was said, sent in that order (the client's checkbox ids: owner 0, hide 1).</summary>
[WiredBox("wf_trg_says_something")]
public sealed class SaysKeywordTrigger : WiredTrigger
{
    private const int OwnerOnly = 0;
    private const int MatchMode = 1;
    private const int HideMessage = 2;

    /// <summary>The message contains the text.</summary>
    private const int Contains = 0;

    /// <summary>The message is the text.</summary>
    private const int Exact = 1;

    /// <summary>"Match all text": any message at all; the client disables the text box for it.</summary>
    private const int AllText = 2;

    public override int Code => TriggerCodes.AvatarSaysSomething;
    /// <summary>A new box on Habbo: anyone, hiding the message.</summary>
    public override IReadOnlyList<int> DefaultIntParams => [0, Contains, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.UserSaid, User: { } user, Text: { } text }
        && (!box.Flag(OwnerOnly) || room.IsOwner(user))
        && TextMatches(box.Settings.StringParam, text, box.IntParam(MatchMode))
        // every capture must have a capturer in the stack that can take what it caught, or the trigger does not fire
        && (box.IntParam(MatchMode) == AllText || VariableCapturerAddon.CanTake(box, Captures(box, text), room));

    /// <summary>Whether a match keeps the message from being shown.</summary>
    public static bool Hides(WiredBox box) => box.Flag(HideMessage);

    private static bool TextMatches(string keyword, string text, int mode)
    {
        if (mode == AllText)
            return true;

        keyword = keyword.Trim();
        if (keyword.Length == 0)
            return false;

        // a keyword with captures fits a message with anything standing where they are; Habbo stops hearing one
        // past about 200 characters
        if (keyword.Contains(CaptureOpen, StringComparison.Ordinal))
            return keyword.Length <= MaxCaptureKeywordLength && CaptureMatch(keyword, mode == Exact, Lowered(text)) is { Success: true };

        return mode switch
        {
            Exact => string.Equals(text.Trim(), keyword, StringComparison.OrdinalIgnoreCase),
            _ => text.Contains(keyword, StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>What the keyword's #(name) captures caught in the message, by name, lowercased as Habbo captures
    /// them; none when the keyword has no captures or the message does not fit it.</summary>
    public static IReadOnlyDictionary<string, string> Captures(WiredBox box, string text)
    {
        var keyword = box.Settings.StringParam.Trim();
        if (!keyword.Contains(CaptureOpen, StringComparison.Ordinal) || keyword.Length > MaxCaptureKeywordLength)
            return new Dictionary<string, string>();

        var match = CaptureMatch(keyword, box.IntParam(MatchMode) == Exact, Lowered(text));
        return match is { Success: true }
            ? CaptureNames(keyword).ToDictionary(name => name, name => match.Groups[name].Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>();
    }

    private const string CaptureOpen = "#(";

    /// <summary>The longest keyword with captures Habbo still hears: 191 characters fired, 202 did not, however many
    /// captures they held (46 at most, with the shortest names).</summary>
    public const int MaxCaptureKeywordLength = 200;

    /// <summary>The message as captures read it: Habbo lowercases it first.</summary>
    private static string Lowered(string text) => text.Trim().ToLowerInvariant();

    /// <summary>A capture: a name that starts with a letter, up to 32 long (the client's limit). Anything else in #() is
    /// plain text.</summary>
    private static readonly Regex CaptureToken = new(@"#\(([A-Za-z_]\w{0,31})\)");

    /// <summary>How long one message is matched against one keyword before it counts as not matching.</summary>
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(50);

    /// <summary>The keywords' patterns, built once each; dropped all together past a bound.</summary>
    private static readonly ConcurrentDictionary<(string Keyword, bool Exact), Regex?> Patterns = new();
    private const int MaxPatterns = 1000;

    private static IEnumerable<string> CaptureNames(string keyword) =>
        CaptureToken.Matches(keyword).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>The message matched against the keyword's pattern; null when the match ran out of time.</summary>
    private static Match? CaptureMatch(string keyword, bool exact, string text)
    {
        if (Patterns.Count > MaxPatterns)
            Patterns.Clear();
        if (Patterns.GetOrAdd((keyword, exact), key => CapturePattern(key.Keyword, key.Exact)) is not { } pattern)
            return null;

        try
        {
            return pattern.Match(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    /// <summary>The keyword as a pattern: its text as it is, each capture one word, a capture named again the same
    /// word again (whatever case it is named in). Captures next to each other catch one character each (Habbo: a
    /// longer message never fires), and in an exact match those ending the keyword may catch nothing, for a text
    /// connector's "0=" to read.</summary>
    private static Regex? CapturePattern(string keyword, bool exact)
    {
        var tokens = CaptureToken.Matches(keyword).ToList();
        var adjacent = new HashSet<int>();
        for (var i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Index == tokens[i - 1].Index + tokens[i - 1].Length)
                adjacent.UnionWith([i - 1, i]);
        }

        // the run of adjacent captures the keyword ends with
        var trailing = new HashSet<int>();
        var end = keyword.Length;
        for (var i = tokens.Count - 1; exact && i >= 0 && adjacent.Contains(i) && tokens[i].Index + tokens[i].Length == end; i--)
        {
            trailing.Add(i);
            end = tokens[i].Index;
        }

        var pattern = new StringBuilder(exact ? "^" : string.Empty);
        var at = 0;
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            pattern.Append(Regex.Escape(keyword[at..token.Index]));
            var name = token.Groups[1].Value;
            if (seen.TryGetValue(name, out var first))
                pattern.Append($@"\k<{first}>");
            else
            {
                seen[name] = name;
                pattern.Append(adjacent.Contains(i) ? $@"(?<{name}>\S)" : $@"(?<{name}>\S+)");
                if (trailing.Contains(i))
                    pattern.Append('?');
            }
            at = token.Index + token.Length;
        }

        pattern.Append(Regex.Escape(keyword[at..]));
        if (exact)
            pattern.Append('$');
        return new Regex(pattern.ToString(),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
    }
}
