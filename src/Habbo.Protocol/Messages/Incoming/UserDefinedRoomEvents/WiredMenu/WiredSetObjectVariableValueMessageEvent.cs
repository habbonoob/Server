using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>1698 WiredSetObjectVariableValue — the inspection tab sets (0), gives (1) or takes (2)
/// a variable of a furni, user or the room.</summary>
[Incoming(ClientMessages.WiredSetObjectVariableValueMessageEvent)]
public sealed partial record WiredSetObjectVariableValueMessageEvent(int Target, int ObjectId, string VariableId, int Value, int Action);
