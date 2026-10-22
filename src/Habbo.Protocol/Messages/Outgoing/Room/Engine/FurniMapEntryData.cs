using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One key/value pair of a map-shaped item.</summary>
[WireStruct]
public sealed partial record FurniMapEntryData(string Key, string Value);
