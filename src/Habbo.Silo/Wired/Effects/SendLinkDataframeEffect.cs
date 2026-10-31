using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends a dataframe through its WIRED Links: up to 12 named values, each a number, a variable's value or text,
/// or a custom message (of up to 200 bytes, placeholders filled in). Its dialog saves the rows' keys and messages as
/// the dataframe string, the row count and then three int parameters a row — where its value comes from, the number,
/// and whom a variable is read from (furni, users, the room, the run) — and a variable id a row. Variables are read
/// from the first of the box's variable source furni or users (its second furni slot, its user slot) that holds one; a
/// row whose variable no one holds is left out.</summary>
[WiredBox("wf_act_send_dataframe")]
public sealed class SendLinkDataframeEffect : WiredEffect
{
    private const int RowCount = 0;
    private const int RowWidth = 3;

    /// <summary>Where a row's value comes from (wiredfurni.params.link.source.*; LinkDataframeCodec).</summary>
    public const int Number = 0;
    public const int VariableValue = 1;
    public const int VariableText = 2;
    public const int CustomMessage = 3;

    /// <summary>The int parameters the dialog saves: the row count, then three for each of the 12 rows.</summary>
    public const int IntParamCount = 1 + WiredLinkDataframe.MaxRows * RowWidth;

    public override int Code => EffectCodes.SendLinkDataframe;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [WiredSlot.EitherPicks, WiredSlot.EitherPicks.DefaultingTo(WiredSources.TriggerFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override WiredPickRule? PickRule => WiredLinks.PickRule;
    public override IReadOnlyList<int> DefaultIntParams { get; } = new int[IntParamCount];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
    public override IReadOnlyList<string> DefaultVariableIds { get; } =
        Enumerable.Repeat(WiredLinkDataframe.NoVariable, WiredLinkDataframe.MaxRows).ToList();
    public override int MaxStringLength => FurniWired.StringParamMaxLength;

    public override WiredValidation? Validate(FurniWired settings)
    {
        var ints = settings.IntParams;
        if (ints.Count != IntParamCount || settings.VariableIds.Count != WiredLinkDataframe.MaxRows
            || WiredLinkDataframe.Decode(settings.StringParam, ints[RowCount]) is not { } rows)
            return WiredValidation.Of(WiredLinkDataframe.Invalid);

        var keys = new HashSet<string>();
        for (var row = 0; row < rows.Count; row++)
        {
            var (key, text) = rows[row];
            if (WiredLinkDataframe.KeyError(key) is { } keyError)
                return WiredValidation.Of(keyError);
            var at = 1 + row * RowWidth;
            if (ints[at] is < Number or > CustomMessage || !IsTarget(ints[at + 2]))
                return WiredValidation.Of(WiredLinkDataframe.Invalid);
            if (ints[at] == CustomMessage && WiredLinkDataframe.Utf8Length(text) > WiredLinkDataframe.MaxMessageBytes)
            {
                return new WiredValidation("wiredfurni.error.too_much_text", new Dictionary<string, string>
                    { ["limit"] = WiredLinkDataframe.MaxMessageBytes.ToString(CultureInfo.InvariantCulture) });
            }
            if (!keys.Add(WiredLinkDataframe.NormalizeKey(key)))
                return WiredValidation.Of(WiredLinkDataframe.NameNotUnique);
        }

        return WiredLinkDataframe.StorageError(settings.StringParam, ints, settings.VariableIds) is { } tooLarge
            ? WiredValidation.Of(tooLarge)
            : WiredLinks.RequireLinks(settings);
    }

    private static bool IsTarget(int target) => target is WiredVariableTargets.Furni or WiredVariableTargets.User
        or WiredVariableTargets.Global or WiredVariableTargets.Context;

    public override async Task RunAsync(WiredBox box, WiredRun run)
    {
        if (WiredLinkDataframe.Decode(box.Settings.StringParam, box.IntParam(RowCount)) is not { } saved)
            return;

        var rows = saved.Select((row, index) => Row(box, run, index, row.Key, row.Text)).OfType<WiredLinkRow>().ToList();
        foreach (var link in run.Furni(box).Where(WiredFurni.IsLink).Take(WiredLinks.MaxLinksPerRun).ToList())
            await run.Room.SendLinkAsync(link, dataframe: true, string.Empty, rows, run);
    }

    /// <summary>A row as it goes out: its key, and its value now; null when it reads a variable no one holds.</summary>
    private static WiredLinkRow? Row(WiredBox box, WiredRun run, int row, string key, string text)
    {
        var at = 1 + row * RowWidth;
        key = WiredLinkDataframe.NormalizeKey(key);
        switch (box.IntParam(at))
        {
            case Number:
                return new WiredLinkRow(key, box.IntParam(at + 1), null);
            case CustomMessage:
                return new WiredLinkRow(key, null, run.Fill(text));
        }

        var asText = box.IntParam(at) == VariableText;
        if (WiredVariableUse.Variable(box, run.Room, row) is not { } variable)
            // a text row may name no variable, and sends no text
            return asText ? new WiredLinkRow(key, null, string.Empty) : null;

        var value = Holders(box, run, box.IntParam(at + 2))
            .Select(holder => run.Room.VariableValue(variable, holder, run))
            .FirstOrDefault(value => value is not null);
        if (value is not { } held)
            return null;

        return asText
            ? new WiredLinkRow(key, null, variable.TextConnector.GetValueOrDefault(held)
                                         ?? variable.TextConnectorFallback ?? held.ToString(CultureInfo.InvariantCulture))
            : new WiredLinkRow(key, held, null);
    }

    /// <summary>Whom a row's variable is read from: the variable source furni (the second furni slot), the variable
    /// source users, the room, or the run.</summary>
    private static IEnumerable<WiredHolder> Holders(WiredBox box, WiredRun run, int target) => target switch
    {
        WiredVariableTargets.Furni => run.Furni(box, 1).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.User => run.Users(box).Select(WiredHolder.Of).ToList(),
        WiredVariableTargets.Global => [WiredHolder.Room],
        WiredVariableTargets.Context => [WiredHolder.Context],
        _ => [],
    };
}
