using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>FuseObjectData — a furni of the arena</summary>
[WireStruct]
public sealed partial record FuseObjectData(string Name, int Id, int X, int Y, int XDimension, int YDimension, int Height, int Direction, int Altitude, bool CanStandOn, Habbo.Protocol.Messages.Outgoing.Room.Engine.FurniStuffData StuffData);
