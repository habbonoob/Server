using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What wired boxes may read and do in the room they stand in.</summary>
public interface IWiredRoom
{
    RoomItemStore Items { get; }
    RoomEntityStore Entities { get; }

    /// <summary>The room's clocks, game and teams.</summary>
    RoomGames Games { get; }

    /// <summary>The room's chests, as far as wired may use them.</summary>
    IWiredChests Chests { get; }

    /// <summary>What give reward boxes hand out.</summary>
    Rewards.IWiredRewards Rewards { get; }

    /// <summary>Pulses since the room's wired timers were last reset: what "at given time" and the
    /// time conditions count.</summary>
    int TimerPulses { get; }

    Random Random { get; }

    bool IsOwner(RoomEntityStore.RoomEntity user);

    /// <summary>Whether the user is still in the room: a delayed effect may run after they left.</summary>
    bool IsPresent(RoomEntityStore.RoomEntity user);

    /// <summary>The placed floor or wall item with this id, if it is still in the room.</summary>
    RoomItemStore.PlacedItem? FindItem(int itemId);

    /// <summary>What the selectors of the trigger's stack choose for the event: what a trigger set to listen to its
    /// selectors listens to.</summary>
    (IReadOnlyList<RoomItemStore.PlacedItem> Furni, IReadOnlyList<RoomEntityStore.RoomEntity> Users) SelectedFor(WiredBox trigger, WiredEvent e);

    /// <summary>The wired stack on a tile, lowest box first; null when there is none.</summary>
    IReadOnlyList<WiredBox>? StackAt(int x, int y);

    /// <summary>The picked furni that are still in the room.</summary>
    IEnumerable<RoomItemStore.PlacedItem> Picked(WiredBox box);

    /// <summary>Stores the item's new state and shows it to everyone.</summary>
    Task SetStateAsync(RoomItemStore.PlacedItem item, string state);

    /// <summary>Moves and turns a floor item where furniture may stand (and, unless told otherwise,
    /// where no avatar stands); false when it may not.</summary>
    Task<bool> MoveItemAsync(RoomItemStore.PlacedItem item, int x, int y, int rotation, WiredMoveOptions options);

    /// <summary>Whether a floor item may be moved there and turned so, as <see cref="MoveItemAsync"/> would.</summary>
    bool CanMoveItem(RoomItemStore.PlacedItem item, int x, int y, int rotation, WiredMoveOptions options);

    /// <summary>The time now, for the time and date conditions.</summary>
    DateTimeOffset Now();

    /// <summary>Runs something so many pulses from now (a teleport's move, after its sparkle).</summary>
    void AfterPulses(int pulses, Func<Task> action);

    /// <summary>An avatar effect's id, by its name in the effect map.</summary>
    Task<int?> FindEffectAsync(string library);

    /// <summary>The room's id.</summary>
    int RoomId { get; }

    /// <summary>The room's group, if it has one.</summary>
    int? GroupId { get; }

    /// <summary>Dresses the user in an effect (0 takes it off).</summary>
    Task<bool> SetEffectAsync(RoomEntityStore.RoomEntity user, int effectId);

    /// <summary>Makes the user dance a style (0 stops), as they could themselves.</summary>
    Task<bool> DanceAsync(RoomEntityStore.RoomEntity user, int style);

    /// <summary>Makes the user hold up a sign, as they could themselves.</summary>
    Task<bool> ShowSignAsync(RoomEntityStore.RoomEntity user, int sign);

    // ---------- variables ----------

    Variables.WiredVariable? FindVariable(string id);
    bool HasVariable(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run);
    int? VariableValue(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run);
    Variables.WiredVariableValue? VariableTimes(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run);
    Task<bool> GiveVariableAsync(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run, int value, bool overrideExisting);
    Task<bool> SetVariableAsync(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run, int value);
    Task<bool> RemoveVariableAsync(Variables.WiredVariable variable, Variables.WiredHolder holder, WiredRun? run);
    IEnumerable<(Variables.WiredHolder Holder, int Value)> VariableHolders(Variables.WiredVariable variable);

    /// <summary>The time zone the room's wired tells time in, for boxes that name none.</summary>
    string Timezone { get; }

    /// <summary>Writes a line to the room's wired log.</summary>
    Task LogAsync(byte level, byte source, string message);

    /// <summary>Lifts or lowers a floor item to an altitude; false when it may not stand there.</summary>
    Task<bool> SetAltitudeAsync(RoomItemStore.PlacedItem item, double altitude);

    /// <summary>Puts the user straight onto the tile, stopping any walk — or keeping it on to the
    /// given goal. A freeze that ends on teleporting ends.</summary>
    void WarpUser(RoomEntityStore.RoomEntity user, int x, int y, (int X, int Y)? keepWalkingTo = null);

    /// <summary>Slides the user one step onto a tile, if they may stand there.</summary>
    void SlideUser(RoomEntityStore.RoomEntity user, int x, int y, int animationMilliseconds, int? jumpPower = null);

    void TurnUser(RoomEntityStore.RoomEntity user, int direction);

    /// <summary>Freezes the user in one of the wired freeze effects (by its option number).</summary>
    Task FreezeAsync(RoomEntityStore.RoomEntity user, int effectOption, bool unfreezeOnTeleport);

    void Unfreeze(RoomEntityStore.RoomEntity user);

    /// <summary>Mutes the user in the room for so many minutes; 0 lifts a mute.</summary>
    void Mute(RoomEntityStore.RoomEntity user, int minutes);

    /// <summary>Sends the user out of the room.</summary>
    Task KickAsync(RoomEntityStore.RoomEntity user);

    /// <summary>Furni moved by wired ran into the user: the collision triggers hear it.</summary>
    Task CollideAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item);

    /// <summary>Shows a message in the wired bubble to the user, or to everyone in the room.</summary>
    void ShowMessage(RoomEntityStore.RoomEntity user, string text, int style, bool toEveryone);

    void ResetTimers();

    /// <summary>Sends a signal through the antenna to the stacks that receive on it, carrying the
    /// furni and users; the sending run's user stays the user it is about.</summary>
    Task SendSignalAsync(RoomItemStore.PlacedItem antenna, IReadOnlyList<RoomItemStore.PlacedItem> furni,
        IReadOnlyList<RoomEntityStore.RoomEntity> users, WiredRun from);

    /// <summary>Tells the user's client what their clicks do in the room (<see cref="Effects.ClickConfigurationEffect"/>).</summary>
    void SetClickSettings(RoomEntityStore.RoomEntity user, int userOption, int furniOption);

    /// <summary>Sends the user straight through the teleport, out of its partner: in this room, or in the one it
    /// stands in. Nothing happens when it leads nowhere.</summary>
    Task SendThroughAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem teleport);

    // ---------- temporary furni ----------

    /// <summary>Places a temporary copy of a floor item at (x, y), at the altitude given or on what is there, and
    /// shows it; null when it cannot stand there.</summary>
    Task<RoomItemStore.PlacedItem?> PlaceCopyAsync(RoomItemStore.PlacedItem template, FurniDataView data, int x, int y, double? z, int rotation);

    /// <summary>Takes a temporary copy out of the room; false for anything else.</summary>
    Task<bool> RemoveCopyAsync(RoomItemStore.PlacedItem copy);

    // ---------- bots ----------

    /// <summary>Starts the entity walking to a tile, or as close as it gets; false when it cannot set off.</summary>
    bool WalkTo(RoomEntityStore.RoomEntity entity, int x, int y);

    /// <summary>A bot says something: to the room, shouted, or (talked or whispered) only to the listeners given.</summary>
    Task BotSayAsync(RoomEntityStore.RoomEntity bot, string text, Abstractions.Chat.RoomChatKind kind, int bubbleWidth,
        IReadOnlyList<RoomEntityStore.RoomEntity>? listeners);

    /// <summary>A bot puts on a look, and keeps it.</summary>
    Task DressBotAsync(RoomEntityStore.RoomEntity bot, string figure);

    /// <summary>Puts a hand item in the user's hand, handed over by the giver if there is one.</summary>
    Task HandOverAsync(RoomEntityStore.RoomEntity? giver, RoomEntityStore.RoomEntity receiver, int handItem);

    /// <summary>A bot walks up next to the user, then does something if they are still next to each other.</summary>
    Task BringAsync(RoomEntityStore.RoomEntity bot, RoomEntityStore.RoomEntity user, Func<Task> then);

    /// <summary>Sets the bot following the user, or (null) following no one.</summary>
    void Follow(RoomEntityStore.RoomEntity bot, RoomEntityStore.RoomEntity? user);

    /// <summary>Runs the effects of the wired stack on the item's tile, as "execute stacks" does:
    /// its triggers and conditions are passed over.</summary>
    Task RunStackEffectsAsync(RoomItemStore.PlacedItem item, WiredRun run);
}
