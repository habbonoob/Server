using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>The room's variable displays as they are now: on entering, and when one is put down or saved.</summary>
[Message(ServerMessages.VariableFxConfigsMessageComposer)]
public sealed partial record VariableFxConfigsMessageComposer(IReadOnlyList<VariableFxConfigData> Configs);
