using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3660 WiredAllVariablesHashComposer — the hash of the room's variables; the client asks for
/// the differences when its own differs.</summary>
[Message(ServerMessages.WiredAllVariablesHashComposer)]
public sealed partial record WiredAllVariablesHashComposer(int Hash);
