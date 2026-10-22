using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>3848 WiredGetAllVariablesHash — what is the room's variables' hash now?</summary>
[Incoming(ClientMessages.WiredGetAllVariablesHashMessageEvent)]
public sealed partial record WiredGetAllVariablesHashMessageEvent;
