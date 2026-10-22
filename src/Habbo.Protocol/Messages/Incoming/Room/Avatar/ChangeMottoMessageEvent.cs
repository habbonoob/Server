using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>2066 ChangeMotto.</summary>
[Incoming(ClientMessages.ChangeMottoMessageEvent)]
public sealed partial record ChangeMottoMessageEvent(string Motto);
