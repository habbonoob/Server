using System.Diagnostics;
using Habbo.Protocol.Frames;
using Habbo.Protocol.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages;

/// <summary>Routes decoded frames to their handlers: pre-crypto handshake handlers first,
/// then the main registry with authentication gating. Headers are the client's release's, read as the server's own
/// (Session.Headers) before routing, and payloads are read in the client's release's layout.</summary>
public sealed class MessageDispatcher(
    MessageHandlerRegistry registry, IOptions<GameNetworkOptions> options, ILogger<MessageDispatcher> logger)
{
    public async Task<bool> DispatchAsync(GatewaySessionContext context, GameFrame frame, CancellationToken ct)
    {
        if (!Allowed(context.Session))
        {
            logger.LogWarning("Connection {ConnectionId}: more than {Rate} messages a second; closing it",
                context.Session.Id, options.Value.MessagesPerSecond);
            return false;
        }

        var known = context.Session.Headers.TryIncoming(frame.Header, out var header);

        if (!context.Crypto.Complete)
        {
            var handshakeHandler = known ? registry.FindPreCrypto(header) : null;
            if (handshakeHandler is null)
            {
                logger.LogWarning("Connection {ConnectionId}: unexpected pre-handshake message {Header}",
                    context.Session.Id, frame.Header);
                return false;
            }

            return await InvokeAsync(context, handshakeHandler, frame, ct);
        }

        if (!known)
        {
            logger.LogDebug("Connection {ConnectionId}: message {Header} of release {Release} has no native counterpart ({Bytes} bytes)",
                context.Session.Id, frame.Header, context.Session.Headers.Release, frame.Payload.Length);
            return true;
        }

        var handler = registry.FindPostCrypto(header);
        if (handler is null)
        {
            logger.LogDebug("Connection {ConnectionId}: unhandled message {Header} ({Bytes} bytes)",
                context.Session.Id, header, frame.Payload.Length);
            return true;
        }

        if (context.Session.Player is null && !MessageHandlerRegistry.AllowsUnauthenticated(handler))
        {
            logger.LogWarning("Connection {ConnectionId}: message {Header} before authentication",
                context.Session.Id, header);
            return false;
        }

        return await InvokeAsync(context, handler, frame, ct);
    }

    /// <summary>Takes one message from the session's allowance, topped up at the allowed rate since the last; false
    /// when it has none left. One client flooding a room's grain would slow the room for everyone in it.</summary>
    private bool Allowed(Session session)
    {
        var (rate, burst) = (options.Value.MessagesPerSecond, options.Value.MessageBurst);
        if (rate <= 0)
            return true;

        var now = Stopwatch.GetTimestamp();
        var allowance = double.IsNaN(session.MessageAllowance)
            ? burst
            : Math.Min(burst, session.MessageAllowance + Stopwatch.GetElapsedTime(session.MessageAllowanceAt, now).TotalSeconds * rate);
        session.MessageAllowanceAt = now;
        session.MessageAllowance = allowance - 1;
        return allowance >= 1;
    }

    private async Task<bool> InvokeAsync(GatewaySessionContext context, IMessageHandler handler, GameFrame frame, CancellationToken ct)
    {
        try
        {
            return await handler.HandleAsync(context, new MessageReader(frame.Payload) { Release = context.Session.Headers.Release }, ct);
        }
        catch (Exception ex) when (GrainHostUnavailable.Is(ex))
        {
            // what it asked of is away for a moment (the rooms' silo being deployed): the message goes unanswered,
            // and the player stays
            logger.LogWarning("Connection {ConnectionId}: handler {Handler} reached no silo: {Reason}",
                context.Session.Id, handler.GetType().Name, ex.Message);
            return true;
        }
        catch (Exception ex)
        {
            // A failing handler closes the connection rather than desyncing the stream.
            logger.LogError(ex, "Connection {ConnectionId}: handler {Handler} failed",
                context.Session.Id, handler.GetType().Name);
            return false;
        }
    }
}
