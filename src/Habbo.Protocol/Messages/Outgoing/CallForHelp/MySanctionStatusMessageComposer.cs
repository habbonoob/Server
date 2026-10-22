using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>MySanctionStatusMessageComposer</summary>
[Message(ServerMessages.MySanctionStatusMessageComposer)]
public sealed partial record MySanctionStatusMessageComposer(IReadOnlyList<SanctionStatusData> Sanctions);
