using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>1743 PlaceObject — the placement is a single space-separated string.</summary>
[Incoming(ClientMessages.PlaceObjectMessageEvent)]
public sealed partial record PlaceObjectMessageEvent(string Placement);
