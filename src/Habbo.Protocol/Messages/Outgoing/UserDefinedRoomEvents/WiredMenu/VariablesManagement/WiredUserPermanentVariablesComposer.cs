using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>3055 WiredUserPermanentVariablesComposer — one user's permanent variables in the room. A
/// pet or bot also names its owner; a Habbo does not.</summary>
[Message(ServerMessages.WiredUserPermanentVariablesComposer)]
public sealed partial record WiredUserPermanentVariablesComposer(
    int EntityType,
    int EntityId,
    string EntityName,
    string EntityFigure,
    [property: WireWhen("HasOwner")] int? OwnerId,
    [property: WireWhen("HasOwner")] string? OwnerName,
    [property: WireWhen("HasOwner")] string? OwnerFigure,
    IReadOnlyList<WiredNamedVariableStorageData> Variables)
{
    public const int HabboType = 1;

    public bool HasOwner => EntityType != HabboType;
}
