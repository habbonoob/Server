using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>3034 UseWallItem — UseFurniture for a wall item.</summary>
[Incoming(ClientMessages.UseWallItemMessageEvent)]
public sealed partial record UseWallItemMessageEvent(int ItemId, int Param);
