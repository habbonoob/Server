using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>How the bot boxes name their bot: the box's text before a tab, the rest of it (a message, a look) after
/// it, as the client writes them.</summary>
public static class WiredBots
{
    private const char Separator = '\t';

    /// <summary>The longest bot name the dialogs take.</summary>
    public const int MaxNameLength = 32;

    public static string Name(WiredBox box) => box.Settings.StringParam.Split(Separator)[0].Trim();

    /// <summary>What follows the name: only when there is exactly that one part, as the client reads it.</summary>
    public static string Rest(WiredBox box) =>
        box.Settings.StringParam.Split(Separator) is [_, var rest] ? rest : string.Empty;

    /// <summary>Whether the entity is a bot of the name the box gives.</summary>
    public static bool Named(WiredBox box, RoomEntityStore.RoomEntity entity) =>
        entity.Bot is { } bot && Name(box) is { Length: > 0 } name && string.Equals(bot.Data.Name, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a bot trigger listens to the bot: the one it names, or one its stack's selectors choose for this
    /// event, as its user source says.</summary>
    public static bool Listens(WiredBox trigger, RoomEntityStore.RoomEntity bot, WiredEvent e, IWiredRoom room) =>
        trigger.UserSource() == WiredSources.SelectedUsers ? room.SelectedFor(trigger, e).Users.Contains(bot) : Named(trigger, bot);

    /// <summary>The bots in the box's bot slot: whatever its source gives that is a bot.</summary>
    public static List<RoomEntityStore.RoomEntity> Of(WiredBox box, WiredRun run) =>
        run.Users(box).Where(entity => entity.Bot is not null).ToList();
}
