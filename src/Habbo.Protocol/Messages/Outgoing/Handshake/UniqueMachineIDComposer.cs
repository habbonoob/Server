using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>UniqueMachineIDComposer — the machine id the client should keep</summary>
[Message(ServerMessages.UniqueMachineIDComposer)]
public sealed partial record UniqueMachineIDComposer(string MachineId);
