using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the users in the room with the names in its text, one name per line (the
/// dialog sends the lines separated by tabs).</summary>
[WiredBox("wf_slc_users_byname")]
public sealed class UsersByNameSelector : WiredUserSelector
{
    private static readonly char[] Separators = ['\t', '\n', '\r'];

    public override int Code => SelectorCodes.UsersByName;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run)
    {
        var names = box.Settings.StringParam
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return run.Room.Entities.Entities.Where(user => names.Contains(user.Player.Username));
    }
}
