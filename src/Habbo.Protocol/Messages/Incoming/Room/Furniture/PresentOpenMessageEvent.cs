using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>PresentOpenMessageEvent — open a present</summary>
[Incoming(ClientMessages.PresentOpenMessageEvent)]
public sealed partial record PresentOpenMessageEvent(int ItemId);
