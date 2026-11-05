using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>1951 UniqueID — the client's machine id, its fingerprint and its Flash version
/// (HabboCommunicationDemo: <c>new UniqueIDMessageComposer(machineId, fingerprint, version)</c>).
///
/// A client that has no machine id yet sends it as null, and EvaWireFormat writes nothing for a null
/// (<c>EvaWireFormat.encode</c>: a null is no String, int, Boolean, ...): only the fingerprint and the version arrive.
/// So two strings mean "no machine id", and the hotel gives the client one (UniqueMachineID).</summary>
[Incoming(ClientMessages.UniqueIDMessageEvent)]
public sealed partial record UniqueIDMessageEvent(string First, string Second, string? Third = null)
{
    /// <summary>The machine id the client keeps, or empty when it sent none.</summary>
    public string MachineId => Third is null ? string.Empty : First;

    public string Fingerprint => Third is null ? First : Second;

    public string FlashVersion => Third ?? Second;
}
