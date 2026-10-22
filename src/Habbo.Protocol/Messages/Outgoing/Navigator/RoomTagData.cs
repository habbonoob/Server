using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>One entry of the tag cloud.</summary>
[WireStruct]
public sealed partial record RoomTagData(string Tag, int RoomCount);
