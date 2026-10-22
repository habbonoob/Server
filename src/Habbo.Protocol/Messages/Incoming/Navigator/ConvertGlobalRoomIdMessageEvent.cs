using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>ConvertGlobalRoomIdMessageEvent</summary>
[Incoming(ClientMessages.ConvertGlobalRoomIdMessageEvent)]
public sealed partial record ConvertGlobalRoomIdMessageEvent(string GlobalId);
