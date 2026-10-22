using Habbo.Protocol.IO;

namespace Habbo.Protocol.Messages;

/// <summary>One incoming message: its header, and how to read it off the wire. Implemented
/// by the generated readers, so a handler never touches a MessageReader.</summary>
public interface IIncomingMessage<out TSelf>
    where TSelf : IIncomingMessage<TSelf>
{
    /// <summary>The post-crypto header the client sends this under.</summary>
    static abstract ushort Header { get; }

    /// <summary>Reads the message's fields, in wire order.</summary>
    static abstract TSelf Read(MessageReader reader);
}
