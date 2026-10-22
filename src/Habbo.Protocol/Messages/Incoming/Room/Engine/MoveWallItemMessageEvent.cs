using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>1614 MoveWallItem — the new wall location string.</summary>
[Incoming(ClientMessages.MoveWallItemMessageEvent)]
public sealed partial record MoveWallItemMessageEvent(int ItemId, string Location);
