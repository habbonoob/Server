using Habbo.Domain.Furniture;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>2125 PickupObject — the leading value distinguishes wall from floor; the last says the player already
/// confirmed a pickup the room asked about (ObjectRemoveConfirm).</summary>
[Incoming(ClientMessages.PickupObjectMessageEvent)]
public sealed partial record PickupObjectMessageEvent(int ItemType, int ItemId, bool Confirmed);
