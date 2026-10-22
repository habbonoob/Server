using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>Variable display values gone, by key: "display id|variable id|u|room index", or "...|f|furni id".</summary>
[Message(ServerMessages.VariableFxStatusRemovedMessageComposer)]
public sealed partial record VariableFxStatusRemovedMessageComposer(IReadOnlyList<string> StatusKeys);
