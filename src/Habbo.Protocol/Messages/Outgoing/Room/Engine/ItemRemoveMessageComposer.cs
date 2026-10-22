using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>761 ItemRemoveMessageComposer — the id as a string, then the picker.</summary>
[Message(ServerMessages.ItemRemoveMessageComposer)]
public sealed partial record ItemRemoveMessageComposer(
    [property: WireFormat(WireType.IntegerString)] int ItemId,
    int PickerAccountId,
    /// <summary>Whether the room's invisible-furni manager took it away; the client
    /// passes it to the object's disposal.</summary>
    [property: WireSince(ClientReleases.October2026)] bool ByInvisibleFurniManager = false);
