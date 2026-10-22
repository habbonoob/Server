using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One changed tile: its position as bytes, then its packed height.</summary>
[WireStruct]
public sealed partial record HeightMapTileWireData(byte X, byte Y, short Value);
