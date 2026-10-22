using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>842 UpdateCondition — a condition's dialog saved. The quantifier says whether all or any of its targets must pass.</summary>
[Incoming(ClientMessages.UpdateConditionMessageEvent)]
public sealed partial record UpdateConditionMessageEvent(
    int ItemId,
    IReadOnlyList<int> IntParams,
    string StringParam,
    IReadOnlyList<int> StuffIds,
    int Quantifier,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> StuffIds2);
