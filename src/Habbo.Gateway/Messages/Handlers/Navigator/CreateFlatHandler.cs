using Habbo.Domain.Rooms;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Silo.Abstractions.Navigator;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>1671 CreateFlat — the navigator's Create Room flow. The client sends the layout as
/// "model_" + the layout name it showed, which is exactly how room models are named.</summary>
internal sealed class CreateFlatHandler(IGrainFactory grains, IRoomDirectoryService directory) : IMessageHandler<CreateFlatMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, CreateFlatMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        if (!await directory.EnsureCanCreateRoomAsync(player))
            return true;

        var request = new RoomCreationRequest(
            message.Name, message.Description, message.ModelName,
            message.CategoryId, message.MaxVisitors, message.TradeMode);

        var created = await grains.GetGrain<INavigatorGrain>(0).CreateRoomAsync(player, request);
        if (created is null)
            return true;

        await context.SendAsync(new FlatCreatedComposer(created.RoomId, created.Name), ct);
        return true;
    }
}
