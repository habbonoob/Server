using System.Globalization;
using System.Text.RegularExpressions;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Triggers;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Captures part of what was said into a variable: the stack's keyword trigger holds a
/// #(name) where the value goes, and whatever stands there — a number, or a text the variable's
/// text connector knows — becomes the variable's value for the one who said it (their user
/// variable, the item, the room, or the run).</summary>
[WiredBox("wf_xtra_text_input_variable")]
public sealed partial class VariableCapturerAddon : WiredAddon
{
    private const int TextMode = 0;
    private const int MaxNameLength = 40;

    public override int Code => AddonCodes.VariableCapturer;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override WiredValidation? Validate(FurniWired settings)
    {
        if (settings.StringParam.Length is < 1 or > MaxNameLength)
            return WiredValidation.Of("wiredfurni.error.placeholder.name_length");
        return NameSyntax().IsMatch(settings.StringParam) ? null : WiredValidation.Of("wiredfurni.error.placeholder.name_syntax");
    }

    /// <summary>A second capturer in the stack under the same name (names are lowercased, so case does not tell
    /// them apart).</summary>
    public override WiredValidation? ValidateInStack(FurniWired settings, WiredBox self, IReadOnlyList<WiredBox> stack) =>
        stack.Any(box => box != self && box.Type is VariableCapturerAddon
                         && string.Equals(box.Settings.StringParam, settings.StringParam, StringComparison.OrdinalIgnoreCase))
            ? WiredValidation.Of("wiredfurni.error.capturer.duplicate")
            : null;

    /// <summary>Whether the stack can take everything the keyword caught: each capture has a capturer of its name,
    /// and what it caught is a number, or in text mode a text the variable's connector maps. Habbo does not fire the
    /// trigger otherwise.</summary>
    public static bool CanTake(WiredBox trigger, IReadOnlyDictionary<string, string> captures, IWiredRoom room)
    {
        if (captures.Count == 0)
            return true;

        var capturers = (room.StackAt(trigger.Item.X, trigger.Item.Y) ?? []).Where(box => box.Type is VariableCapturerAddon).ToList();
        foreach (var (name, caught) in captures)
        {
            if (capturers.FirstOrDefault(c => string.Equals(c.Settings.StringParam, name, StringComparison.OrdinalIgnoreCase)) is not { } capturer
                || WiredVariableUse.Variable(capturer, room) is not { } variable
                || Value(capturer, variable, caught) is null)
                return false;
        }

        return true;
    }

    /// <summary>The value what was caught stands for: a number, or in text mode the value the variable's connector
    /// gives that text, an exact match first and then one in any case; null when it stands for none.</summary>
    private static int? Value(WiredBox addon, WiredVariable variable, string caught)
    {
        if (!addon.Flag(TextMode))
            return int.TryParse(caught, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

        foreach (var comparison in (StringComparison[])[StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase])
        {
            foreach (var (value, text) in variable.TextConnector)
            {
                if (string.Equals(text, caught, comparison))
                    return value;
            }
        }

        return null;
    }

    /// <summary>Stores what the capture caught, for a run started by what was said.</summary>
    public static async Task CaptureAsync(WiredBox addon, IReadOnlyList<WiredBox> triggers, WiredRun run)
    {
        if (run.Event is not { Type: WiredEventType.UserSaid, Text: { } text }
            || WiredVariableUse.Variable(addon, run.Room) is not { } variable)
            return;

        foreach (var trigger in triggers.Where(t => t.Type is SaysKeywordTrigger))
        {
            if (!SaysKeywordTrigger.Captures(trigger, text).TryGetValue(addon.Settings.StringParam, out var caught))
                continue;

            if (Value(addon, variable, caught) is not { } value)
                return;

            WiredHolder? holder = variable.Target switch
            {
                WiredVariableTargets.User when run.User is { } user => WiredHolder.Of(user),
                WiredVariableTargets.Furni when run.Item is { } item => WiredHolder.Of(item),
                WiredVariableTargets.Global => WiredHolder.Room,
                WiredVariableTargets.Context => WiredHolder.Context,
                _ => null,
            };
            if (holder is { } h)
            {
                if (variable.AlwaysAvailable)
                    await run.Room.SetVariableAsync(variable, h, run, value);
                else
                    await run.Room.GiveVariableAsync(variable, h, run, value, overrideExisting: true);
            }

            return;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex NameSyntax();
}
