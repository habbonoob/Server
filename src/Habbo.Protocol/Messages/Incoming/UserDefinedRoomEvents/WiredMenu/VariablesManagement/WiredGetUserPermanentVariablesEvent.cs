using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>2292 WiredGetUserPermanentVariables — one user's permanent variables in the room.</summary>
[Incoming(ClientMessages.WiredGetUserPermanentVariablesEvent)]
public sealed partial record WiredGetUserPermanentVariablesEvent(int EntityType, int EntityId);
