using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>Gives the stack's texts a placeholder, $(name), for the chosen variable's value in its
/// holders — as a number, or as the text a text connector gives the value.</summary>
[WiredBox("wf_xtra_text_output_variable")]
public sealed class VariablePlaceholderAddon : WiredPlaceholderAddon
{
    private const int Target = 1;
    private const int TextMode = 2;

    public override int Code => AddonCodes.VariablePlaceholder;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams => [0, WiredVariableTargets.User, 0];

    protected override IEnumerable<string> Values(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return [];

        return WiredVariableUse.Holders(box, run, box.IntParam(Target))
            .Select(holder => run.Room.VariableValue(variable, holder, run))
            .OfType<int>()
            .Select(value => box.Flag(TextMode) && variable.TextConnector.TryGetValue(value, out var text)
                ? text
                : value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
