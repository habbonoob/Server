namespace Habbo.Gateway.Messages;

/// <summary>Binds a handler class to an incoming message header. Only needed by a handler that
/// reads the payload itself; implementing IMessageHandler&lt;T&gt; takes the header from T.
/// Repeatable — one class may handle several messages.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MessageHandlerAttribute(ushort header) : Attribute
{
    public ushort Header { get; } = header;
}

/// <summary>Marks a handler as reachable before the RC4 leg is up. These are the handshake
/// messages; everything else is only dispatched once the ciphers are installed.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PreCryptoAttribute : Attribute;

/// <summary>Marks a post-crypto handler as callable before authentication; the default requires a logged-in player.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AllowUnauthenticatedAttribute : Attribute;
