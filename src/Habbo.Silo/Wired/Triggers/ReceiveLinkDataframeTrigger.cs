using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a link dataframe arrives on one of its WIRED Links, and captures the values its rows name: each
/// row matches an incoming key to a text placeholder, or to a writable context variable of the room's own. Its dialog
/// saves the rows' keys and placeholder names as the dataframe string, the row count and then what each row captures
/// into (0 a placeholder, 1 a variable), and a variable id a row. A key the dataframe lacks captures nothing; the
/// trigger fires all the same.</summary>
[WiredBox("wf_trg_receive_dataframe")]
public sealed class ReceiveLinkDataframeTrigger : WiredTrigger
{
    private const int RowCount = 0;

    /// <summary>What a row captures into (wiredfurni.params.link.destination.*).</summary>
    public const int ToPlaceholder = 0;
    public const int ToVariable = 1;

    /// <summary>The int parameters the dialog saves: the row count, then one for each of the 12 rows.</summary>
    public const int IntParamCount = 1 + WiredLinkDataframe.MaxRows;

    private const int MaxPlaceholderLength = 40;
    private const string ReservedPlaceholder = Wired.WiredRun.TriggerTextPlaceholder;
    private const string InvalidCaptureVariable = "wiredfurni.error.link.invalid_capture_variable";
    private const string DestinationConflict = "wiredfurni.error.link.destination_conflict";

    public override int Code => TriggerCodes.ReceiveLinkDataframe;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredLinks.PickRule;
    public override IReadOnlyList<int> DefaultIntParams { get; } = new int[IntParamCount];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
    public override IReadOnlyList<string> DefaultVariableIds { get; } =
        Enumerable.Repeat(WiredLinkDataframe.NoVariable, WiredLinkDataframe.MaxRows).ToList();
    public override int MaxStringLength => FurniWired.StringParamMaxLength;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.LinkDataframeReceived, Item: { } link } && Watches(box, link, e, room);

    public override WiredValidation? Validate(FurniWired settings)
    {
        var ints = settings.IntParams;
        if (ints.Count != IntParamCount || settings.VariableIds.Count != WiredLinkDataframe.MaxRows
            || WiredLinkDataframe.Decode(settings.StringParam, ints[RowCount]) is not { } rows)
            return WiredValidation.Of(WiredLinkDataframe.Invalid);

        var keys = new HashSet<string>();
        var destinations = new HashSet<string>();
        for (var row = 0; row < rows.Count; row++)
        {
            var (key, placeholder) = rows[row];
            if (WiredLinkDataframe.KeyError(key) is { } keyError)
                return WiredValidation.Of(keyError);
            if (ints[1 + row] == ToPlaceholder && PlaceholderError(placeholder) is { } placeholderError)
                return WiredValidation.Of(placeholderError);
            if (ints[1 + row] is not (ToPlaceholder or ToVariable))
                return WiredValidation.Of(WiredLinkDataframe.Invalid);
            if (!keys.Add(WiredLinkDataframe.NormalizeKey(key)))
                return WiredValidation.Of(WiredLinkDataframe.NameNotUnique);
            if (!destinations.Add(Destination(ints[1 + row], placeholder, settings.VariableIds[row])))
                return WiredValidation.Of(DestinationConflict);
        }

        return WiredLinkDataframe.StorageError(settings.StringParam, ints, settings.VariableIds) is { } tooLarge
            ? WiredValidation.Of(tooLarge)
            : WiredLinks.RequireLinks(settings);
    }

    /// <summary>Every row capturing into a variable names a context variable the room's users made, which holds a value
    /// and may be written (the dialog's own filter).</summary>
    public override Task<WiredValidation?> ValidateAsync(FurniWired settings, IWiredRoom room)
    {
        var rows = WiredLinkDataframe.Decode(settings.StringParam, settings.IntParams[RowCount])!;
        for (var row = 0; row < rows.Count; row++)
        {
            if (settings.IntParams[1 + row] == ToVariable
                && room.FindVariable(settings.VariableIds[row]) is not
                    { Kind: WiredVariableKinds.CreatedByUser, Target: WiredVariableTargets.Context, HasValue: true, CanCreateAndDelete: true, CanWriteValue: true })
                return Task.FromResult<WiredValidation?>(WiredValidation.Of(InvalidCaptureVariable));
        }

        return Task.FromResult<WiredValidation?>(null);
    }

    /// <summary>"Each capturer must be unique in this wired stack": no two receiving triggers of a stack capture into
    /// the same placeholder or variable.</summary>
    public override WiredValidation? ValidateInStack(FurniWired settings, WiredBox self, IReadOnlyList<WiredBox> stack)
    {
        var mine = Destinations(settings).ToHashSet();
        return stack.Any(box => box != self && box.Type is ReceiveLinkDataframeTrigger && Destinations(box.Settings).Any(mine.Contains))
            ? WiredValidation.Of(DestinationConflict)
            : null;
    }

    /// <summary>Captures what the dataframe that set the trigger off carries for its rows: a placeholder takes the text
    /// (a number written out), a context variable the number (a text that is one).</summary>
    public static async Task CaptureAsync(WiredBox trigger, WiredRun run)
    {
        if (run.Event is not { Type: WiredEventType.LinkDataframeReceived } e
            || WiredLinkDataframe.Decode(trigger.Settings.StringParam, trigger.IntParam(RowCount)) is not { } rows)
            return;

        for (var row = 0; row < rows.Count; row++)
        {
            var key = WiredLinkDataframe.NormalizeKey(rows[row].Key);
            if (e.LinkRows.FirstOrDefault(incoming => incoming.Key == key) is not { } incoming)
                continue;

            if (trigger.IntParam(1 + row) == ToPlaceholder)
            {
                run.Placeholders[rows[row].Text] = incoming.Text ?? incoming.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                continue;
            }

            var value = incoming.Number
                        ?? (int.TryParse(incoming.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null);
            if (value is null || WiredVariableUse.Variable(trigger, run.Room, row) is not { Target: WiredVariableTargets.Context } variable)
                continue;

            if (variable.AlwaysAvailable)
                await run.Room.SetVariableAsync(variable, WiredHolder.Context, run, value.Value);
            else
                await run.Room.GiveVariableAsync(variable, WiredHolder.Context, run, value.Value, overrideExisting: true);
        }
    }

    private static IEnumerable<string> Destinations(FurniWired settings)
    {
        if (settings.IntParams.Count != IntParamCount || settings.VariableIds.Count != WiredLinkDataframe.MaxRows
            || WiredLinkDataframe.Decode(settings.StringParam, settings.IntParams[RowCount]) is not { } rows)
            return [];
        return rows.Select((row, index) => Destination(settings.IntParams[1 + index], row.Text, settings.VariableIds[index]));
    }

    private static string Destination(int kind, string placeholder, string variableId) =>
        kind == ToPlaceholder ? "text:" + placeholder.ToLowerInvariant() : "variable:" + variableId;

    /// <summary>Why a placeholder name may not be captured into (LinkDataframeCodec.placeholderError).</summary>
    private static string? PlaceholderError(string name)
    {
        if (name.Length is 0 or > MaxPlaceholderLength)
            return "wiredfurni.error.placeholder.name_length";
        if (!name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            return "wiredfurni.error.placeholder.name_syntax";
        return string.Equals(name, ReservedPlaceholder, StringComparison.OrdinalIgnoreCase) ? "wiredfurni.error.placeholder.reserved" : null;
    }
}
