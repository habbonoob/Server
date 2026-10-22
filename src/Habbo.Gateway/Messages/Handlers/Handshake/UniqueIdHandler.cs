using Habbo.Protocol.Messages.Incoming.Handshake;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>1951 UniqueID — the machine id and fingerprint the client generates. The machine id
/// is kept for the login, which a machine ban turns away.</summary>
[AllowUnauthenticated]
internal sealed class UniqueIdHandler(ILogger<UniqueIdHandler> logger)
    : IMessageHandler<UniqueIDMessageEvent>
{
    /// <summary>A machine id is 32 hex digits, as the client makes its own.</summary>
    private const int MachineIdBytes = 16;

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, UniqueIDMessageEvent message, CancellationToken ct)
    {
        // a client with none is given one, which it keeps for next time
        if (string.IsNullOrWhiteSpace(message.MachineId))
        {
            context.Session.MachineId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(MachineIdBytes)).ToLowerInvariant();
            await context.SendAsync(new Habbo.Protocol.Messages.Outgoing.Handshake.UniqueMachineIDComposer(context.Session.MachineId), ct);
        }
        else
            context.Session.MachineId = message.MachineId;
        logger.LogDebug("Connection {ConnectionId}: machine={MachineId}, fingerprint={Fingerprint}",
            context.Session.Id, message.MachineId, message.Fingerprint);
        return true;
    }
}
