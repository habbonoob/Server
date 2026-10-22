namespace Habbo.Silo.Wired;

/// <summary>The group a group box means: the one its dialog chose (the text holds its id), or with none chosen the
/// room's own.</summary>
public static class WiredGroups
{
    public static int? Chosen(WiredBox box, IWiredRoom room) =>
        box.Settings.StringParam.Trim() is { Length: > 0 } chosen
            ? int.TryParse(chosen, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : null
            : room.GroupId;
}
