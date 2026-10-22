using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>478 UpdateTrigger — a trigger's dialog saved.</summary>
[Incoming(ClientMessages.UpdateTriggerMessageEvent)]
public sealed partial record UpdateTriggerMessageEvent(
    int ItemId,
    IReadOnlyList<int> IntParams,
    string StringParam,
    IReadOnlyList<int> StuffIds,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> StuffIds2);
