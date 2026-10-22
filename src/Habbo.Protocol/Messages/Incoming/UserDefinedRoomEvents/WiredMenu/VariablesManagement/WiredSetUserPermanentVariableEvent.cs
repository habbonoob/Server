using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>714 WiredSetUserPermanentVariable — sets (0), gives (1) or takes (2) one of a user's
/// permanent variables, whether or not they are here.</summary>
[Incoming(ClientMessages.WiredSetUserPermanentVariableEvent)]
public sealed partial record WiredSetUserPermanentVariableEvent(int EntityType, int EntityId, string VariableId, int Value, int Action);
