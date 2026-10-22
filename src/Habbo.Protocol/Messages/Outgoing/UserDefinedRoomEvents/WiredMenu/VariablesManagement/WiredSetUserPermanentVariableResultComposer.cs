using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>3339 WiredSetUserPermanentVariableResultComposer — whether a change to a user's variable worked.</summary>
[Message(ServerMessages.WiredSetUserPermanentVariableResultComposer)]
public sealed partial record WiredSetUserPermanentVariableResultComposer(bool Success);
