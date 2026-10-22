using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>167 CurrentTimingCodeMessageComposer — which code is running for a schedule the
/// client asked about. The scheduling name comes back with it because several widgets ask at
/// once and each only takes the answer to its own question.</summary>
[Message(ServerMessages.CurrentTimingCodeMessageComposer)]
public sealed partial record CurrentTimingCodeMessageComposer(string SchedulingKey, string Code);
