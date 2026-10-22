using System.Globalization;

namespace Habbo.Silo.Wired.Addons;

/// <summary>In a variable box's stack, gives the variable's values texts ("0=text0", a line each),
/// which text placeholders show and text captures read.</summary>
[WiredBox("wf_xtra_var_text_connector")]
public sealed class VariableTextConnectorAddon : WiredAddon
{
    public override int Code => AddonCodes.VariableTextConnector;

    public static IReadOnlyDictionary<int, string> Texts(WiredBox box)
    {
        var texts = new Dictionary<int, string>();
        foreach (var line in box.Settings.StringParam.Split('\n'))
        {
            var equals = line.IndexOf('=');
            if (equals > 0 && int.TryParse(line[..equals].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                texts[value] = line[(equals + 1)..].TrimEnd('\r');
        }

        return texts;
    }
}
