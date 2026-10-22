using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Layout;

/// <summary>GetOccupiedTilesMessageEvent</summary>
[Incoming(ClientMessages.GetOccupiedTilesMessageEvent)]
public sealed partial record GetOccupiedTilesMessageEvent;
