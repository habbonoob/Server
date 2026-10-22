using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredOpenContractMessageEvent — a contract's requirements.</summary>
internal sealed class WiredOpenContractHandler : IMessageHandler<WiredOpenContractMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredOpenContractMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendContractAsync(player.Id, message.ContractId);
        return true;
    }
}
