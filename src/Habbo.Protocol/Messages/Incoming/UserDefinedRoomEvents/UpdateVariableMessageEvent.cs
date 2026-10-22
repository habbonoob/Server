using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>2511 UpdateVariable — a variable box's dialog saved.</summary>
[Incoming(ClientMessages.UpdateVariableMessageEvent)]
public sealed partial record UpdateVariableMessageEvent(
    int ItemId,
    IReadOnlyList<int> IntParams,
    string StringParam,
    IReadOnlyList<int> StuffIds,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> StuffIds2);
