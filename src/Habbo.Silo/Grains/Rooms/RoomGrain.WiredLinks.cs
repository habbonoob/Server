using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Microsoft.EntityFrameworkCore;
using Result = Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink.WiredLinkOperationResultMessageComposer;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>WIRED Links (wf_link): the furni that connect rooms' wired. A Link's owner connects it, in its dialog, to one
/// of their networks — named groups of their Links, kept per user, one Link per room each — and says whether it also
/// receives what is sent through it in its own room. What the room's send-link effects send through a Link reaches
/// the other Links of its network, in whichever rooms they stand, and the Link itself when it receives its own room's.
///
/// Only rooms someone is in hear what is sent: a room that is not loaded has no wired running to hear it, and Habbo is
/// taken not to load one to deliver a message (unmeasured). Only the network's owner's Links count, so a Link given
/// away connects no one.</summary>
public sealed partial class RoomGrain
{
    /// <summary>How many networks one user may have (wiredlink.error.8). Habbo's limit is unmeasured.</summary>
    private const int MaxWiredLinkNetworks = 20;

    /// <summary>How long where a network leads (its Links in other rooms with someone in them) is kept before it is
    /// looked up again: a repeating stack would otherwise ask the database every time it sends.</summary>
    private static readonly TimeSpan WiredLinkTargetsLifetime = TimeSpan.FromSeconds(5);

    private const string LinkNotOwner = "wiredfurni.error.link.not_owner";

    /// <summary>The connections of the room's Links that have been set up, by item, by its database id as they are
    /// saved; the client names a Link by the id the room knows it by, and a delivery from another room by its
    /// database id.</summary>
    private readonly Dictionary<int, WiredLinkEndpoint> _linkEndpoints = [];

    /// <summary>Where each network the room's Links send on leads, by network, and when that was looked up.</summary>
    private readonly Dictionary<int, (DateTimeOffset At, List<(int RoomId, int ItemId)> Targets)> _linkTargets = [];

    private static bool IsWiredLink(RoomItemStore.PlacedItem item) => item.Interaction is WiredLinkInteraction;

    private async Task LoadWiredLinksAsync(AppDbContext db, CancellationToken ct)
    {
        var ids = _items!.Items.Where(IsWiredLink).Select(item => item.Item.Id).ToList();
        if (ids.Count == 0)
            return;

        foreach (var endpoint in await db.WiredLinkEndpoints.AsNoTracking().Where(e => ids.Contains(e.ItemId)).ToListAsync(ct))
            _linkEndpoints[endpoint.ItemId] = endpoint;
    }

    /// <summary>A Link put down starts disconnected: one that left a room without being picked up (its room deleted, say)
    /// may still have a connection saved.</summary>
    private async Task WiredLinkPlacedAsync(RoomItemStore.PlacedItem placed)
    {
        if (!IsWiredLink(placed))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.WiredLinkEndpoints.Where(e => e.ItemId == placed.Item.Id).ExecuteDeleteAsync();
        _linkEndpoints.Remove(placed.Item.Id);
    }

    /// <summary>Links picked up are disconnected ("to disconnect a Link, pick it up").</summary>
    private async Task WiredLinksTakenAsync(AppDbContext db, IReadOnlyCollection<int> itemIds)
    {
        var links = itemIds.Where(_linkEndpoints.ContainsKey).ToList();
        if (links.Count == 0)
            return;

        await db.WiredLinkEndpoints.Where(e => links.Contains(e.ItemId)).ExecuteDeleteAsync();
        foreach (var id in links)
            _linkEndpoints.Remove(id);
    }

    /// <summary>Using a Link opens its dialog for its owner; anyone else is told only its owner may.</summary>
    private bool OpenWiredLinkOnUse(RoomEntityStore.RoomEntity entity, RoomItemStore.PlacedItem item)
    {
        if (!IsWiredLink(item))
            return false;

        SendTo(entity.Player.Id, item.Item.OwnerId == entity.Player.Id
            ? new WiredLinkOpenMessageComposer(item.Id)
            : WiredDialogs.Refusal(WiredValidation.Of(LinkNotOwner)));
        return true;
    }

    // ---------- the dialog ----------

    public async Task OpenWiredLinkAsync(Guid playerId, int itemId)
    {
        if (OwnedLink(playerId, itemId) is not { } link)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var endpoint = _linkEndpoints.GetValueOrDefault(link.Item.Id);
        SendTo(playerId, new WiredLinkConfigurationMessageComposer(_room!.Id, link.Id, NetworkText(endpoint?.NetworkId),
            endpoint?.ReceiveLocal ?? false, await NetworksAsync(db, playerId)));
    }

    public async Task SaveWiredLinkAsync(Guid playerId, int itemId, string networkId, bool receiveLocal)
    {
        if (LinkFor(playerId, itemId, Result.Save) is not { } link)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        int? network = null;
        if (networkId != Result.NoNetwork)
        {
            if (ParseNetwork(networkId) is not { } id)
            {
                LinkFailed(playerId, itemId, Result.Save, Result.InvalidNetworkId);
                return;
            }

            if (!await db.WiredLinkNetworks.AnyAsync(n => n.Id == id && n.OwnerId == playerId))
            {
                LinkFailed(playerId, itemId, Result.Save, Result.NetworkNotFound);
                return;
            }

            // one Link per network in a room
            var roomId = _room!.Id;
            var storedId = link.Item.Id;
            if (await db.WiredLinkEndpoints.AnyAsync(e => e.NetworkId == id && e.ItemId != storedId && e.Item!.RoomId == roomId))
            {
                LinkFailed(playerId, itemId, Result.Save, Result.NetworkLinkedHere);
                return;
            }

            network = id;
        }

        var linkId = link.Item.Id;
        var before = _linkEndpoints.GetValueOrDefault(linkId)?.NetworkId;
        var row = await db.WiredLinkEndpoints.FirstOrDefaultAsync(e => e.ItemId == linkId);
        if (network is null && !receiveLocal)
        {
            // connected to nothing and hearing nothing: the Link as it was before it was ever set up
            if (row is not null)
                db.WiredLinkEndpoints.Remove(row);
            _linkEndpoints.Remove(linkId);
        }
        else
        {
            if (row is null)
            {
                row = new WiredLinkEndpoint { ItemId = linkId };
                db.WiredLinkEndpoints.Add(row);
            }

            row.NetworkId = network;
            row.ReceiveLocal = receiveLocal;
            _linkEndpoints[linkId] = new WiredLinkEndpoint { ItemId = linkId, NetworkId = network, ReceiveLocal = receiveLocal };
        }

        await db.SaveChangesAsync();
        _linkTargets.Clear();

        var changed = new[] { before, network }.OfType<int>().Distinct().ToList();
        SendTo(playerId, new Result(_room!.Id, link.Id, Result.Save, Result.Done, true, NetworkText(network), receiveLocal,
            Result.NoNetwork, changed.Count == 0 ? [] : await NetworksAsync(db, playerId, changed)));
    }

    public async Task CreateWiredLinkNetworkAsync(Guid playerId, int itemId, string name)
    {
        if (LinkFor(playerId, itemId, Result.Create) is not { } link)
            return;

        name = name.Trim();
        if (!IsNetworkName(name))
        {
            LinkFailed(playerId, itemId, Result.Create, Result.InvalidName);
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var names = await db.WiredLinkNetworks.Where(n => n.OwnerId == playerId).Select(n => n.Name).ToListAsync();
        if (names.Count >= MaxWiredLinkNetworks)
        {
            LinkFailed(playerId, itemId, Result.Create, Result.NetworkLimit);
            return;
        }

        if (names.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            LinkFailed(playerId, itemId, Result.Create, Result.DuplicateName);
            return;
        }

        var network = new WiredLinkNetwork { OwnerId = playerId, Name = name };
        db.WiredLinkNetworks.Add(network);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // the same name made at the same moment from another room
            LinkFailed(playerId, itemId, Result.Create, Result.DuplicateName);
            return;
        }

        SendTo(playerId, new Result(_room!.Id, link.Id, Result.Create, Result.Done, false, null, null, Result.NoNetwork,
            await NetworksAsync(db, playerId, [network.Id])));
    }

    public async Task RenameWiredLinkNetworkAsync(Guid playerId, int itemId, string networkId, string name)
    {
        if (LinkFor(playerId, itemId, Result.Rename) is not { } link)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await OwnedNetworkAsync(db, playerId, itemId, networkId, Result.Rename) is not { } network)
            return;

        name = name.Trim();
        if (!IsNetworkName(name))
        {
            LinkFailed(playerId, itemId, Result.Rename, Result.InvalidName);
            return;
        }

        var others = await db.WiredLinkNetworks.Where(n => n.OwnerId == playerId && n.Id != network.Id).Select(n => n.Name).ToListAsync();
        if (others.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            LinkFailed(playerId, itemId, Result.Rename, Result.DuplicateName);
            return;
        }

        network.Name = name;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            LinkFailed(playerId, itemId, Result.Rename, Result.DuplicateName);
            return;
        }

        SendTo(playerId, new Result(_room!.Id, link.Id, Result.Rename, Result.Done, false, null, null, Result.NoNetwork,
            await NetworksAsync(db, playerId, [network.Id])));
    }

    /// <summary>Deletes the network, which disconnects its Links in every room; the loaded rooms holding one are told,
    /// so their owner's open dialog drops it.</summary>
    public async Task DeleteWiredLinkNetworkAsync(Guid playerId, int itemId, string networkId)
    {
        if (LinkFor(playerId, itemId, Result.Delete) is not { } link)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await OwnedNetworkAsync(db, playerId, itemId, networkId, Result.Delete) is not { } network)
            return;

        var roomId = _room!.Id;
        var rooms = await db.WiredLinkEndpoints.Where(e => e.NetworkId == network.Id && e.Item!.RoomId != null && e.Item.RoomId != roomId)
            .Select(e => e.Item!.RoomId!.Value).Distinct().ToListAsync();

        // the database disconnects the Links (wired_link_endpoints.network_id is set null with it)
        await db.WiredLinkNetworks.Where(n => n.Id == network.Id).ExecuteDeleteAsync();
        NetworkDeleted(network.Id);

        var endpoint = _linkEndpoints.GetValueOrDefault(link.Item.Id);
        SendTo(playerId, new Result(roomId, link.Id, Result.Delete, Result.Done, true, NetworkText(endpoint?.NetworkId),
            endpoint?.ReceiveLocal ?? false, NetworkText(network.Id), []));

        var populated = await GrainFactory.GetGrain<IRoomPopulationGrain>(0).GetAsync();
        foreach (var other in rooms.Where(populated.ContainsKey))
            await GrainFactory.GetGrain<IGameRoomGrain>(other).WiredLinkNetworkDeletedAsync(network.Id);
    }

    public async Task GetWiredLinkConnectionsAsync(Guid playerId, int itemId, string networkId)
    {
        if (LinkFor(playerId, itemId, Result.Connections) is not { } link)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await OwnedNetworkAsync(db, playerId, itemId, networkId, Result.Connections) is not { } network)
            return;

        // saved connections in unloaded rooms too; a Link in an inventory is no longer connected. A Link here goes by
        // the id the room knows it by, as the dialog does; one in another room by its database id, as this room
        // cannot know the id its own room gives Builders Club furni (the list says which those are).
        var roomId = _room!.Id;
        var connections = (await db.WiredLinkEndpoints.AsNoTracking()
            .Where(e => e.NetworkId == network.Id && e.Item!.RoomId != null)
            .OrderBy(e => e.Item!.RoomId).ThenBy(e => e.ItemId)
            .Select(e => new WiredLinkConnectionData(e.Item!.RoomId!.Value, e.Item.Room!.Name, e.ItemId, e.Item.BuildersClub, e.ReceiveLocal))
            .ToListAsync())
            .Select(c => c.RoomId == roomId ? c with { ItemId = _items!.IdOfStored(c.ItemId) } : c)
            .ToList();
        SendTo(playerId, new WiredLinkConnectionsMessageComposer(roomId, link.Id, NetworkText(network.Id), connections));
    }

    public Task WiredLinkNetworkDeletedAsync(int networkId)
    {
        NetworkDeleted(networkId);
        return Task.CompletedTask;
    }

    /// <summary>The room's Links on a deleted network are disconnected, and each one's owner, if here, told.</summary>
    private void NetworkDeleted(int networkId)
    {
        _linkTargets.Remove(networkId);
        foreach (var endpoint in _linkEndpoints.Values.Where(e => e.NetworkId == networkId).ToList())
        {
            endpoint.NetworkId = null;
            if (_items?.FindStored(endpoint.ItemId) is { } link && _entities?.Find(link.Item.OwnerId) is not null)
                SendTo(link.Item.OwnerId, new WiredLinkInvalidatedMessageComposer(_room!.Id, link.Id, NetworkText(networkId)));
        }
    }

    // ---------- what goes through ----------

    /// <summary>Sends a payload through one of the room's Links: back to the Link when it receives its own room's, and to
    /// the other Links of its network, in the rooms someone is in.</summary>
    private async Task SendThroughWiredLinkAsync(RoomItemStore.PlacedItem link, WiredLinkPayload payload)
    {
        if (_room is null || _wired is null || _linkEndpoints.GetValueOrDefault(link.Item.Id) is not { } endpoint)
            return;

        if (endpoint.ReceiveLocal)
            await _wired.LinkReceivedAsync(link, payload);

        if (endpoint.NetworkId is not { } networkId)
            return;

        foreach (var (roomId, itemId) in await WiredLinkTargetsAsync(networkId, link.Item.OwnerId))
            await GrainFactory.GetGrain<IGameRoomGrain>(roomId).ReceiveWiredLinkAsync(new WiredLinkDelivery(itemId, networkId, payload));
    }

    /// <summary>The Links of the network in other rooms someone is in: only when the sending Link's owner owns the
    /// network, and only Links the network's owner owns.</summary>
    private async Task<List<(int RoomId, int ItemId)>> WiredLinkTargetsAsync(int networkId, Guid ownerId)
    {
        var now = DateTimeOffset.UtcNow;
        if (_linkTargets.GetValueOrDefault(networkId) is { } known && now - known.At < WiredLinkTargetsLifetime)
            return known.Targets;

        await using var db = await dbFactory.CreateDbContextAsync();
        var roomId = _room!.Id;
        var links = await db.WiredLinkEndpoints.AsNoTracking()
            .Where(e => e.NetworkId == networkId && e.Network!.OwnerId == ownerId && e.Item!.OwnerId == ownerId
                        && e.Item.RoomId != null && e.Item.RoomId != roomId)
            .Select(e => new { RoomId = e.Item!.RoomId!.Value, e.ItemId })
            .ToListAsync();
        var populated = links.Count == 0 ? new Dictionary<int, int>() : await GrainFactory.GetGrain<IRoomPopulationGrain>(0).GetAsync();
        var targets = links.Where(l => populated.ContainsKey(l.RoomId)).Select(l => (l.RoomId, l.ItemId)).ToList();
        _linkTargets[networkId] = (now, targets);
        return targets;
    }

    /// <summary>A payload sent from another room arrived for one of this room's Links, named by its database id. It is
    /// dropped when the Link is gone or no longer on the network, or nobody is here to hear it (the room was loaded
    /// only to take it).</summary>
    public async Task ReceiveWiredLinkAsync(WiredLinkDelivery delivery)
    {
        if (_wired is null || _entities is not { Count: > 0 } || _items?.FindStored(delivery.ItemId) is not { } link || !IsWiredLink(link)
            || _linkEndpoints.GetValueOrDefault(delivery.ItemId)?.NetworkId != delivery.NetworkId)
            return;

        await _wired.LinkReceivedAsync(link, delivery.Payload);
    }

    // ---------- helpers ----------

    /// <summary>The Link, when the player is here and owns it; a player who does not is told so, as the dialog's
    /// operation failing.</summary>
    private RoomItemStore.PlacedItem? LinkFor(Guid playerId, int itemId, int operation)
    {
        if (_room is null || _entities?.Find(playerId) is null)
            return null;
        if (_items?.Find(itemId) is not { } link || !IsWiredLink(link))
        {
            LinkFailed(playerId, itemId, operation, Result.LinkGone);
            return null;
        }

        if (link.Item.OwnerId != playerId)
        {
            LinkFailed(playerId, itemId, operation, Result.NotOwner);
            return null;
        }

        return link;
    }

    /// <summary>The Link, for opening its dialog: only its owner, here, may.</summary>
    private RoomItemStore.PlacedItem? OwnedLink(Guid playerId, int itemId) =>
        _room is not null && _entities?.Find(playerId) is not null && _items?.Find(itemId) is { } link && IsWiredLink(link)
        && link.Item.OwnerId == playerId
            ? link
            : null;

    /// <summary>The player's network the dialog names, tracked for changing; the dialog is told why not otherwise.</summary>
    private async Task<WiredLinkNetwork?> OwnedNetworkAsync(AppDbContext db, Guid playerId, int itemId, string networkId, int operation)
    {
        if (ParseNetwork(networkId) is not { } id)
        {
            LinkFailed(playerId, itemId, operation, Result.InvalidNetworkId);
            return null;
        }

        if (await db.WiredLinkNetworks.FirstOrDefaultAsync(n => n.Id == id && n.OwnerId == playerId) is not { } network)
        {
            LinkFailed(playerId, itemId, operation, Result.NetworkNotFound);
            return null;
        }

        return network;
    }

    /// <summary>The player's networks (or those of them named), as the dialog lists them: how many Links each has, and
    /// whether one stands in this room.</summary>
    private async Task<List<WiredLinkNetworkData>> NetworksAsync(AppDbContext db, Guid playerId, IReadOnlyCollection<int>? only = null)
    {
        var roomId = _room!.Id;
        var networks = await db.WiredLinkNetworks.AsNoTracking()
            .Where(n => n.OwnerId == playerId && (only == null || only.Contains(n.Id)))
            .OrderBy(n => n.Id)
            .Select(n => new
            {
                n.Id,
                n.Name,
                Count = db.WiredLinkEndpoints.Count(e => e.NetworkId == n.Id),
                Here = db.WiredLinkEndpoints.Any(e => e.NetworkId == n.Id && e.Item!.RoomId == roomId),
            })
            .ToListAsync();
        return networks.Select(n => new WiredLinkNetworkData(NetworkText(n.Id), n.Name, n.Count, n.Here)).ToList();
    }

    private void LinkFailed(Guid playerId, int itemId, int operation, int resultCode) =>
        SendTo(playerId, Result.Failed(_room?.Id ?? 0, itemId, operation, resultCode));

    /// <summary>A network's id as the client knows it: its decimal string, "0" for none.</summary>
    private static string NetworkText(int? networkId) =>
        networkId?.ToString(CultureInfo.InvariantCulture) ?? Result.NoNetwork;

    private static int? ParseNetwork(string networkId) =>
        int.TryParse(networkId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;

    /// <summary>A name with some text in it, of no more than 255 characters.</summary>
    private static bool IsNetworkName(string name) =>
        name.Length is > 0 and <= WiredLinkNetwork.MaxNameLength && name.Any(c => !char.IsWhiteSpace(c));
}
