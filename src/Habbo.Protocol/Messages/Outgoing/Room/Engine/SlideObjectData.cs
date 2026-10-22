using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>SlideObjectData — something a roller moves: its id and its height before and after</summary>
[WireStruct]
public sealed partial record SlideObjectData(int Id, string FromZ, string ToZ);
