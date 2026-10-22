using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>1951 UniqueID — the machine fingerprint the client generates.</summary>
[Incoming(ClientMessages.UniqueIDMessageEvent)]
public sealed partial record UniqueIDMessageEvent(string MachineId, string Fingerprint);
