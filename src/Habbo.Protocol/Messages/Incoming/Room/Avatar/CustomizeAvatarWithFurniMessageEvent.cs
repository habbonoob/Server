using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>1461 CustomizeAvatarWithFurni — put on what a furni dresses you in (a costume stand).</summary>
[Incoming(ClientMessages.CustomizeAvatarWithFurniMessageEvent)]
public sealed partial record CustomizeAvatarWithFurniMessageEvent(int ItemId);
