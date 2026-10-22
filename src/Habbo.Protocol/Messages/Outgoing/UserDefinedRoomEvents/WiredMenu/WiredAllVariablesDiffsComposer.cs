using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3858 WiredAllVariablesDiffsComposer — a chunk of the differences between the client's
/// variables and the room's: the room's hash, whether this is the last chunk, the ids gone, and
/// the variables added or changed.</summary>
[Message(ServerMessages.WiredAllVariablesDiffsComposer)]
public sealed partial record WiredAllVariablesDiffsComposer(
    int AllVariablesHash,
    bool IsLastChunk,
    IReadOnlyList<string> RemovedVariableIds,
    IReadOnlyList<WiredVariableWithHashData> AddedOrUpdated);
