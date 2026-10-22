using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>ShowEnforceRoomCategoryDialogComposer — the owner must pick a category the room may have.</summary>
[Message(ServerMessages.ShowEnforceRoomCategoryDialogComposer)]
public sealed partial record ShowEnforceRoomCategoryDialogComposer(int SelectionType)
{
    /// <summary>The client shows the one dialog whatever the type, offering every category the owner may pick.</summary>
    public const int AnyCategory = 0;
}
