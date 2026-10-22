using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>2693 UpdateAction — an effect's dialog saved. The delay is in pulses of half a second.</summary>
[Incoming(ClientMessages.UpdateActionMessageEvent)]
public sealed partial record UpdateActionMessageEvent(
    int ItemId,
    IReadOnlyList<int> IntParams,
    string StringParam,
    IReadOnlyList<int> StuffIds,
    int DelayInPulses,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> StuffIds2);
