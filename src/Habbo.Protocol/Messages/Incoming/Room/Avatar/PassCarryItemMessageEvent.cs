using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>647 PassCarryItem — hand what the avatar carries to another user.</summary>
[Incoming(ClientMessages.PassCarryItemMessageEvent)]
public sealed partial record PassCarryItemMessageEvent(int UserId);
