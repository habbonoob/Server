using Habbo.Protocol.IO;

namespace Habbo.Protocol.Composers;

/// <summary>One outgoing message: a header plus its wire layout. Implemented by the generated
/// message records, which are the only way a message is written.</summary>
public interface IComposer
{
    ushort Header { get; }
    void Compose(MessageWriter w);

    /// <summary>Whether a client release lays this message out differently ([WireSince], [WireUntil] on it or on
    /// a struct it holds), so that each release needs its own payload.</summary>
    bool VariesByRelease => false;
}
