using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>How every wired dialog opens (the client's Triggerable): the box, the settings it was
/// last saved with, and what the dialog lets the player pick. <see cref="Code"/> chooses the
/// dialog's layout; <see cref="StuffTypeId"/> is the box's furnidata id, for its icon.</summary>
[WireStruct]
public sealed partial record WiredBoxData(
    int FurniLimit,
    IReadOnlyList<int> StuffIds,
    IReadOnlyList<int> StuffIds2,
    int StuffTypeId,
    int ItemId,
    string StringParam,
    IReadOnlyList<int> IntParams,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    int Code);
