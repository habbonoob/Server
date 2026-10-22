using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway;

/// <summary>A composed outbound message queued for the session's writer task, which writes it in the session's
/// client release's header and layout. Encrypted is decided when the message is enqueued: frames sent before the
/// handshake completes (1142, 1038) go out plaintext.</summary>
public sealed record OutboundFrame(ComposedMessage Message, bool Encrypted);
