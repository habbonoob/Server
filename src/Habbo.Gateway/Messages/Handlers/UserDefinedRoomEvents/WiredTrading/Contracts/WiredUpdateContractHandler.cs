using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredUpdateContractMessageEvent — save a contract.</summary>
internal sealed class WiredUpdateContractHandler : IMessageHandler<WiredUpdateContractMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredUpdateContractMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.UpdateContractAsync(player.Id, ContractUpdates.From(message));
        return true;
    }
}
