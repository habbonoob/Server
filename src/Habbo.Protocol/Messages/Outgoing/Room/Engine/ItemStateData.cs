using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ItemStateData</summary>
[WireStruct]
public sealed partial record ItemStateData(int ItemId, string State);
