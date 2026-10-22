using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ObjectRemoveMultipleMessageComposer</summary>
[Message(ServerMessages.ObjectRemoveMultipleMessageComposer)]
public sealed partial record ObjectRemoveMultipleMessageComposer(
    IReadOnlyList<int> ItemIds,
    int PickerAccountId,
    /// <summary>Whether the room's invisible-furni manager took it away; the client
    /// passes it to the object's disposal.</summary>
    [property: WireSince(ClientReleases.October2026)] bool ByInvisibleFurniManager = false);
