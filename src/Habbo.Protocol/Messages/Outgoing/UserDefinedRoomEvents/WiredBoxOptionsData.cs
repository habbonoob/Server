using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>The part of a wired dialog between its type's own fields: whether the advanced
/// section starts open, the target slots with the sources each may use (the client's
/// InputSourcesConf) and their defaults, and whether wall items may be picked.</summary>
[WireStruct]
public sealed partial record WiredBoxOptionsData(
    bool AdvancedMode,
    IReadOnlyList<WiredSourceChoiceData> FurniSlots,
    IReadOnlyList<WiredSourceChoiceData> UserSlots,
    IReadOnlyList<int> DefaultFurniSources,
    IReadOnlyList<int> DefaultUserSources,
    bool AllowWallFurni);
