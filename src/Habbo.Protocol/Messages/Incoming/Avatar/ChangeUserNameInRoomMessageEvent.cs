using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Avatar;

/// <summary>2591 ChangeUserNameInRoom — take this name, from the in-room name change.</summary>
[Incoming(ClientMessages.ChangeUserNameInRoomMessageEvent)]
public sealed partial record ChangeUserNameInRoomMessageEvent(string Name);
