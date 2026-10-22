using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>A variable the client holds, by the hash it cached it with.</summary>
[WireStruct]
public sealed partial record WiredKnownVariableData(string Id, int Hash);

/// <summary>671 WiredGetAllVariablesDiffs — the variables the client holds; it wants the rest.</summary>
[Incoming(ClientMessages.WiredGetAllVariablesDiffsMessageEvent)]
public sealed partial record WiredGetAllVariablesDiffsMessageEvent(IReadOnlyList<WiredKnownVariableData> Known);
