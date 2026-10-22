using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>Values for variable displays; all of them the values they start with when <paramref name="AllInitialize"/>.</summary>
[Message(ServerMessages.VariableFxStatusMessageComposer)]
public sealed partial record VariableFxStatusMessageComposer(bool AllInitialize, IReadOnlyList<VariableFxStatusData> Statuses);
