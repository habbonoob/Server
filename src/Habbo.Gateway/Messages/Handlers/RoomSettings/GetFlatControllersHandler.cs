using Habbo.Protocol.Messages.Incoming.RoomSettings;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.RoomSettings;

/// <summary>3832 GetFlatControllers — [roomId]; only the owner (or staff) is told.</summary>
internal sealed class GetFlatControllersHandler(IGrainFactory grains) : IMessageHandler<GetFlatControllersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetFlatControllersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        if (await grains.GetGrain<IGameRoomGrain>(message.RoomId).GetFlatControllersAsync(player.Id) is { } controllers)
            await context.SendAsync(new FlatControllersComposer(message.RoomId,
                controllers.Select(c => new FlatControllerData(c.AccountId, c.Username)).ToList()), ct);
        return true;
    }
}
