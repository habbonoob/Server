using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>2815 UpdateSelector — a selector's dialog saved. A filter narrows what earlier selectors chose; invert takes everything else.</summary>
[Incoming(ClientMessages.UpdateSelectorMessageEvent)]
public sealed partial record UpdateSelectorMessageEvent(
    int ItemId,
    IReadOnlyList<int> IntParams,
    string StringParam,
    IReadOnlyList<int> StuffIds,
    bool IsFilter,
    bool IsInvert,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> StuffIds2);
