using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>3635 DropCarryItem — put down whatever the avatar carries.</summary>
[Incoming(ClientMessages.DropCarryItemMessageEvent)]
public sealed partial record DropCarryItemMessageEvent;
