using Habbo.Silo.Abstractions.Permissions;
using System.Diagnostics;
using System.Reflection;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Commands;
using Habbo.Silo.Permissions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The chat commands: each by its name and the other words that call it, run for whoever has its permission
/// ("command." and its name, seed/permissions.json). A command the player may not use is only something said.</summary>
public sealed partial class RoomGrain
{
    private Dictionary<string, (ChatCommandSpec Spec, Func<RoomEntityStore.RoomEntity, string, Task> Run)>? _commands;

    /// <summary>Every command by every word that calls it.</summary>
    private Dictionary<string, (ChatCommandSpec Spec, Func<RoomEntityStore.RoomEntity, string, Task> Run)> Commands =>
        _commands ??= BuildCommands();

    private Dictionary<string, (ChatCommandSpec, Func<RoomEntityStore.RoomEntity, string, Task>)> BuildCommands()
    {
        var all = new List<(ChatCommandSpec, Func<RoomEntityStore.RoomEntity, string, Task>)>
        {
            // everyone
            (new("commands", [], ":commands"), (player, _) => ListCommandsAsync(player)),
            (new("about", [], ":about"), (player, _) => AboutAsync(player)),
            (new("sit", [], ":sit"), (player, _) => PostureAsync(player, _entities!.SitHere)),
            (new("lay", [], ":lay"), (player, _) => PostureAsync(player, _entities!.LayHere)),
            (new("stand", [], ":stand"), (player, _) => PostureAsync(player, _entities!.StandHere)),

            // the room's own, which check who may themselves
            (new(PickAllCommand, [], ":pickall"), (player, _) => PickAllAsync(player)),
            (new(EjectAllCommand, [], ":ejectall [username]"), EjectAllAsync),
            (new(EjectPetsCommand, [], ":ejectpets"), (player, _) => EjectPetsAsync(player)),
            (new(ResetScoresCommand, [], ":resetscores"), (player, _) => ResetScoresAsync(player)),

            // the room's owner
            (new("setmax", [], ":setmax <amount>"), SetMaxAsync),
            (new("diagonal", [], ":diagonal"), (owner, _) => ToggleDiagonalAsync(owner)),
            (new("empty", [], ":empty [yes]"), (owner, argument) => EmptyInventoryAsync(owner, argument, InventoryPart.Furni)),
            (new("emptypets", [], ":emptypets [yes]"), (owner, argument) => EmptyInventoryAsync(owner, argument, InventoryPart.Pets)),
            (new("emptybots", [], ":emptybots [yes]"), (owner, argument) => EmptyInventoryAsync(owner, argument, InventoryPart.Bots)),

            // staff
            (new(GiveCommand, [], ":give <currency> <amount> <username, room or hotel>"), GiveCurrencyAsync),
            (new("summon", [], ":summon <username>"), SummonAsync),
            (new("gotoroom", ["goto"], ":gotoroom <room id>"), GoToRoomAsync),
            (new("follow", ["stalk"], ":follow <username>"), FollowAsync),
            (new("teleport", ["tp"], ":teleport"), ToggleTeleportAsync),
            (new("userinfo", ["playerinfo"], ":userinfo <username>"), UserInfoAsync),
            (new("freeze", [], ":freeze <username>"), (staff, argument) => FreezeAsync(staff, argument, frozen: true)),
            (new("unfreeze", [], ":unfreeze <username>"), (staff, argument) => FreezeAsync(staff, argument, frozen: false)),
            (new("disconnect", ["dc"], ":disconnect <username>"), DisconnectPlayerAsync),
            (new("roomalert", ["ra"], ":roomalert <message>"), RoomAlertAsync),
            (new("makesay", [], ":makesay <username> <message>"), MakeSayAsync),
            (new("mute", ["shutup"], ":mute <username> [minutes]"), (staff, argument) => MuteAsync(staff, argument, mute: true)),
            (new("unmute", [], ":unmute <username>"), (staff, argument) => MuteAsync(staff, argument, mute: false)),
            (new("roommute", [], ":roommute"), RoomMuteAsync),
            (new("unload", [], ":unload"), UnloadAsync),
            (new("reloadroom", ["reload_room"], ":reloadroom"), ReloadRoomAsync),
            (new("invisible", ["hideme"], ":invisible"), ToggleInvisibleAsync),

            // staff, about the whole hotel
            (new("ha", [], ":ha <message>"), Hotel(hotelCommands.HotelAlertAsync)),
            (new("hal", [], ":hal <url> <message>"), Hotel(hotelCommands.HotelAlertWithLinkAsync)),
            (new("sa", [], ":sa <message>"), Hotel(hotelCommands.StaffAlertAsync)),
            (new("staffonline", [], ":staffonline"), Hotel((_, _) => hotelCommands.ListStaffOnlineAsync())),
            (new("givebadge", [], ":givebadge <username> <badge>"), Hotel((_, argument) => hotelCommands.BadgeAsync(argument, give: true))),
            (new("takebadge", [], ":takebadge <username> <badge>"), Hotel((_, argument) => hotelCommands.BadgeAsync(argument, give: false))),
            (new("roombadge", [], ":roombadge <badge>"), Hotel((_, argument) => hotelCommands.BadgeManyAsync(argument, PlayersHere()))),
            (new("massbadge", [], ":massbadge <badge>"), Hotel((_, argument) => hotelCommands.BadgeManyAsync(argument, null))),
            (new("roomgift", [], ":roomgift <item> [message]"), Hotel((staff, argument) => hotelCommands.GiftManyAsync(staff, argument, PlayersHere()))),
            (new("massgift", [], ":massgift <item> [message]"), Hotel((staff, argument) => hotelCommands.GiftManyAsync(staff, argument, null))),
            (new("giverank", [], ":giverank <username> <rank>"), Hotel(hotelCommands.GiveRankAsync)),
            (new("happyhour", [], ":happyhour [minutes]"), Hotel((_, argument) => hotelCommands.HappyHourAsync(argument))),
            (new("promoteoffer", [], ":promoteoffer <offer id>"), Hotel((_, argument) => hotelCommands.PromoteOfferAsync(argument))),
            (new("filter", [], ":filter <word> [replacement]"), Hotel((_, argument) => hotelCommands.FilterWordAsync(argument))),
            (new("update", [], ":update <name>"), Hotel((_, argument) => hotelCommands.UpdateAsync(argument))),
        };

        var byWord = new Dictionary<string, (ChatCommandSpec, Func<RoomEntityStore.RoomEntity, string, Task>)>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in all)
        {
            byWord[command.Item1.Name] = command;
            foreach (var alias in command.Item1.Aliases)
                byWord[alias] = command;
        }

        return byWord;
    }

    /// <summary>A hotel command, run as the staff member, who is told its answer.</summary>
    private Func<RoomEntityStore.RoomEntity, string, Task> Hotel(Func<PlayerData, string, Task<string>> run) =>
        async (staff, argument) => TellPlayer(staff, await run(staff.Player, argument));

    /// <summary>Everyone in the room now, the invisible too.</summary>
    private IReadOnlyCollection<Guid> PlayersHere() => _entities!.Avatars.Select(e => e.Player.Id).ToList();

    /// <summary>Runs the command the word calls, if there is one and the player may use it.</summary>
    private async Task<bool> RunCommandAsync(RoomEntityStore.RoomEntity entity, string word, string argument)
    {
        if (!Commands.TryGetValue(word, out var command) || !permissions.Has(entity.Player, PermissionNames.Command(command.Spec.Name)))
            return false;

        await command.Run(entity, argument);
        return true;
    }

    /// <summary>:commands — what the player may use, each as the permissions table describes it.</summary>
    private Task ListCommandsAsync(RoomEntityStore.RoomEntity player)
    {
        var lines = Commands.Values.Select(c => c.Spec).Distinct()
            .Where(spec => permissions.Has(player.Player, PermissionNames.Command(spec.Name)))
            .OrderBy(spec => spec.Name, StringComparer.Ordinal)
            .Select(spec => permissions.DescriptionOf(PermissionNames.Command(spec.Name)) is { Length: > 0 } description ? description : spec.Usage);
        TellPlayer(player, string.Join("\n", lines));
        return Task.CompletedTask;
    }

    /// <summary>:about — the server, how long it has run, and who is about.</summary>
    private async Task AboutAsync(RoomEntityStore.RoomEntity player)
    {
        var assembly = typeof(RoomGrain).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString() ?? "";
        version = version.Split('+')[0];   // without the commit the build was made from
        var up = DateTime.Now - Process.GetCurrentProcess().StartTime;
        var online = (await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync()).Count;
        TellPlayer(player, $"Habbo server {version}, up {(int)up.TotalDays}d {up.Hours}h {up.Minutes}m, {online} online");
    }

    /// <summary>:sit, :lay, :stand — the posture where one stands, for everyone to see.</summary>
    private async Task PostureAsync(RoomEntityStore.RoomEntity player, Func<RoomEntityStore.RoomEntity, AvatarUpdateData?> posture)
    {
        await WakeAsync(player);
        if (posture(player) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));
        await DrainEntityChangesAsync();
    }
}
