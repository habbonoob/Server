using Habbo.Domain.Rooms;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.ChatStyles;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.RaidProtection;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Layout;
using Habbo.Protocol.Messages.Outgoing.Room.Permissions;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Room.Session;

/// <summary>Entering a room: the door, then the entry sequence. The client asks for it with OpenFlatConnection,
/// or the server starts it itself when a navigator forward finds the room open (GetGuestRoom).
/// HeightMap must precede FloorHeightMap: the client's floor handler bails silently without the stacking map,
/// leaving a black room.</summary>
internal static class RoomEntry
{
    /// <summary>The name a doorbell answer carries for the visitor themselves.</summary>
    private const string ToTheVisitor = "";

    private const string VisitDeniedForVisitor = "builders_club.visit_denied_for_visitor";

    public static async Task EnterAsync(
        IGrainFactory grains, GatewaySessionContext context, int roomId, string password, CancellationToken ct)
    {
        try
        {
            await EnterRoomAsync(grains, context, roomId, password, ct);
        }
        catch (Exception ex) when (GrainHostUnavailable.Is(ex))
        {
            // the rooms are away for a moment (their silo being deployed): the client leaves its loading screen for
            // the hotel view, and can try again
            if (context.Session.Room is not null)
            {
                try
                {
                    await context.Session.LeaveRoomAsync();
                }
                catch (Exception again) when (GrainHostUnavailable.Is(again))
                {
                    // the room can't be told: it is away with its silo, avatars and all
                    await context.Session.SetRoomAsync(null);
                }
            }
            await context.SendAsync(new CloseConnectionMessageComposer(), ct);
        }
    }

    private static async Task EnterRoomAsync(
        IGrainFactory grains, GatewaySessionContext context, int roomId, string password, CancellationToken ct)
    {
        var player = context.Session.Player!;

        // entering a room leaves the one the session was in, and whatever game was being played there (Habbo tells
        // it on every move from one room to another, before the new room opens)
        if (context.Session.Room is not null)
        {
            await context.Session.LeaveRoomAsync();
            await context.SendAsync(new YouArePlayingGameMessageComposer(IsPlaying: false), ct);
        }

        var room = grains.GetGrain<IGameRoomGrain>(roomId);
        var access = await room.RequestAccessAsync(player.Id, context.Session.SessionGuid, player, password);
        switch (access)
        {
            case RoomAccess.Allowed:
                break;
            case RoomAccess.Queued:
                // waiting in line is being at the room: leaving the line is leaving it
                await context.Session.SetRoomAsync(room);
                return;
            case RoomAccess.Spectator:
                // the session must know it only watches before the room is built
                await context.SendAsync(new YouAreSpectatorMessageComposer(roomId), ct);
                break;
            case RoomAccess.NoSuchRoom:
                await context.SendAsync(new NoSuchFlatComposer(roomId), ct);
                return;
            case RoomAccess.Full:
                await context.SendAsync(new CantConnectMessageComposer(CantConnectMessageComposer.RoomFull, null), ct);
                return;
            case RoomAccess.Banned:
                await context.SendAsync(new CantConnectMessageComposer(CantConnectMessageComposer.Banned, null), ct);
                return;
            case RoomAccess.WrongPassword:
                await context.SendAsync(new GenericErrorComposer(GenericErrorCodes.WrongPassword), ct);
                return;
            case RoomAccess.Ringing:
                // the client waits, told so by an empty doorbell, until somebody inside answers
                await context.SendAsync(new DoorbellMessageComposer(ToTheVisitor), ct);
                return;
            case RoomAccess.NobodyToOpen:
                await context.SendAsync(new FlatAccessDeniedMessageComposer(roomId, ToTheVisitor), ct);
                return;
            case RoomAccess.NotANewPlayer:
                await context.SendAsync(new CantConnectMessageComposer(CantConnectMessageComposer.Blocked, null), ct);
                return;
            case RoomAccess.LockedByBuildersClub:
                // "This room is locked!", and the client back out of the room it was loading
                await context.SendAsync(new NotificationDialogMessageComposer(VisitDeniedForVisitor, []), ct);
                await context.SendAsync(new CloseConnectionMessageComposer(), ct);
                return;
        }

        var load = access == RoomAccess.Spectator
            ? await room.SpectateAsync(player.Id, context.Session.SessionGuid, player)
            : await room.EnterAsync(player.Id, context.Session.SessionGuid, player);
        if (load is null)
        {
            // The client leaves its loading screen up until it is told the room is not there.
            await context.SendAsync(new NoSuchFlatComposer(roomId), ct);
            return;
        }

        await context.Session.SetRoomAsync(room);
        var owners = OwnerRows(load);

        // The order is Habbo's own room entry, captured from a live session (release 202609091217). The client
        // builds the room when FloorHeightMap arrives and reads whatever it has been told by then: the entry tile,
        // the stacking map and the wall/floor properties. The avatars come in two Users packets: those already here
        // before the furniture, the entrant after it.
        await context.SendAsync(new OpenConnectionMessageComposer(load.RoomId), ct);
        // the hotel has no NFTs, so no chat styles of theirs
        await context.SendAsync(new UserNftChatStylesMessageComposer([]), ct);
        await context.SendAsync(new RoomReadyMessageComposer(load.ModelName, load.RoomId), ct);
        await context.SendAsync(new RoomEntryTileMessageComposer(load.DoorX, load.DoorY, load.DoorDirection), ct);

        foreach (var (key, value) in RoomProperties(load.Visualization))
            await context.SendAsync(new RoomPropertyMessageComposer(key, value), ct);

        await context.SendAsync(HeightMapMessageComposer.From(load.Width, load.Height, load.Tiles), ct);
        await context.SendAsync(FloorHeightMapMessageComposer.From(load.Heightmap, load.Visualization, load.AreaHides), ct);
        await context.SendAsync(RoomVisualizationSettingsComposer.From(load.Visualization), ct);
        await context.SendAsync(UsersMessageComposer.From(load.Entities.Where(e => e.RoomIndex != load.OwnRoomIndex).ToList()), ct);
        await context.SendAsync(new ObjectsMessageComposer(owners, load.FloorItems.Select(item => FloorItemData.From(item, withOwnerName: false)).ToList()), ct);
        await context.SendAsync(new ItemsMessageComposer(owners, load.WallItems.Select(item => WallItemData.From(item, withOwnerName: false)).ToList()), ct);
        await context.SendAsync(UsersMessageComposer.From(load.Entities.Where(e => e.RoomIndex == load.OwnRoomIndex).ToList()), ct);
        await context.SendAsync(new RoomEntryInfoMessageComposer(load.RoomId, load.IsOwner), ct);
        await context.SendAsync(RoomEventComposer.From(load.Event, load.RoomId, DateTimeOffset.UtcNow), ct);
        if (load.Configuration is { } configuration)
            await context.SendAsync(ConfigurationItemStatesMessageComposer.From(configuration), ct);
        await context.SendAsync(new WiredEnvironmentMessageComposer(load.WiredListensForUserClicks, EnabledAchievements: []), ct);
        // the rights come in among the wired packets: the level, the wired permissions, then the ownership (Habbo's
        // own room entry, measured)
        if (load.RoomControllerLevel > RoomControllerLevels.None)
            await context.SendAsync(new YouAreControllerMessageComposer(load.RoomId, load.RoomControllerLevel), ct);
        await context.SendAsync(new WiredPermissionsComposer(load.CanModifyWired, load.CanReadWired), ct);
        if (load.IsOwner)
            await context.SendAsync(new YouAreOwnerMessageComposer(load.RoomId), ct);
        await context.SendAsync(new RoomRatingComposer(load.Score, load.CanRate), ct);
        await context.SendAsync(new RaidProtectionCapabilityComposer(load.RoomId, CanManage: load.IsOwner), ct);
        if (access != RoomAccess.Spectator)
            await room.ShowOthersToAsync(player.Id);

        // where everyone stands and what they do, as the room's next tick would tell it
        await context.SendAsync(UserUpdateMessageComposer.From(load.Statuses), ct);

        if (access != RoomAccess.Spectator)
            await room.EnteredAsync(player.Id);
    }

    /// <summary>The owner-name rows the item packets are preceded by, one per distinct owner:
    /// the client resolves item owner names through this map.</summary>
    private static List<RoomOwnerData> OwnerRows(RoomLoadData load) =>
        load.FloorItems.Concat(load.WallItems)
            .GroupBy(item => item.OwnerAccountId)
            .Select(group => new RoomOwnerData(group.Key, group.First().OwnerName))
            .ToList();

    /// <summary>RoomProperty is one key/value pair per message, in Habbo's order. A floor or wallpaper never put down
    /// is not sent (Habbo, a new room: landscape 0.0 alone), and the client draws its default; the landscape always is.</summary>
    private static IEnumerable<(string Key, string Value)> RoomProperties(RoomVisualizationData visualization)
    {
        if (visualization.Wallpaper.Length > 0)
            yield return ("wallpaper", visualization.Wallpaper);
        if (visualization.FloorType.Length > 0)
            yield return ("floor", visualization.FloorType);
        yield return ("landscape", visualization.Landscape.Length > 0 ? visualization.Landscape : Habbo.Domain.Rooms.Room.NoLandscape);
    }
}
