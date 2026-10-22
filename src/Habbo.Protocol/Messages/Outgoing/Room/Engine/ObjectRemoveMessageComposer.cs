using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1359 ObjectRemoveMessageComposer — the id as a string.</summary>
[Message(ServerMessages.ObjectRemoveMessageComposer)]
public sealed partial record ObjectRemoveMessageComposer(
    [property: WireFormat(WireType.IntegerString)] int ItemId,
    bool IsExpired,
    int PickerAccountId,
    int Delay,
    /// <summary>Whether the room's invisible-furni manager took it away; the client
    /// passes it to the object's disposal.</summary>
    [property: WireSince(ClientReleases.October2026)] bool ByInvisibleFurniManager = false);
