using Habbo.Protocol.IO;
using Habbo.Protocol.Messages;

namespace Habbo.Gateway.Messages;

/// <summary>Handles one incoming message. Return false to close the connection. A room message that comes while the
/// session is in no room (a step sent as the doorbell refused it, say) is ignored, not a reason to close.
/// Implemented directly only by handlers that read nothing off the wire themselves;
/// everything else implements the generic form and lets the reader do the parsing.</summary>
public interface IMessageHandler
{
    Task<bool> HandleAsync(GatewaySessionContext context, MessageReader reader, CancellationToken ct);
}

/// <summary>Handles one incoming message, already parsed. The header comes from the message,
/// so a handler never repeats it.</summary>
public interface IMessageHandler<TMessage>
    where TMessage : IIncomingMessage<TMessage>
{
    Task<bool> HandleAsync(GatewaySessionContext context, TMessage message, CancellationToken ct);
}

/// <summary>Adapts a parsed handler to the dispatcher: reads the message, then hands it over.</summary>
internal sealed class ParsingMessageHandler<TMessage>(IMessageHandler<TMessage> inner)
    : IMessageHandler, IParsingMessageHandler
    where TMessage : IIncomingMessage<TMessage>
{
    /// <summary>The handler this wraps, so the registry can read its attributes.</summary>
    public object Inner => inner;

    public Task<bool> HandleAsync(GatewaySessionContext context, MessageReader reader, CancellationToken ct) =>
        inner.HandleAsync(context, TMessage.Read(reader), ct);
}
