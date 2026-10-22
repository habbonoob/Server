using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Furni;

/// <summary>3765/2918 RequestFurniInventory — the inventory, split into FurniList fragments
/// the client reassembles. 3765 fires in a room, 2918 outside one; same answer.</summary>
internal sealed class RequestFurniInventoryHandler(IGrainFactory grains, IOptions<HotelOptions> hotel) :
    IMessageHandler<RequestFurniInventoryEvent>,
    IMessageHandler<RequestFurniInventoryWhenNotInRoomEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, RequestFurniInventoryEvent message, CancellationToken ct) =>
        SendInventoryAsync(context, ct);

    public Task<bool> HandleAsync(
        GatewaySessionContext context, RequestFurniInventoryWhenNotInRoomEvent message, CancellationToken ct) =>
        SendInventoryAsync(context, ct);

    private async Task<bool> SendInventoryAsync(GatewaySessionContext context, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var items = await grains.GetGrain<IPlayerGrain>(player.Id).GetInventoryAsync();

        // An empty inventory is still one (empty) fragment, or the client never initializes it.
        var fragments = items.Chunk(Math.Max(1, hotel.Value.InventoryFragmentSize)).ToList();
        if (fragments.Count == 0)
            fragments.Add([]);

        for (var index = 0; index < fragments.Count; index++)
            await context.SendAsync(new FurniListComposer(fragments.Count, index, fragments[index].Select(InventoryFurniData.From).ToList()), ct);

        return true;
    }
}
