using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>OpenPetPackageRequestedMessageComposer — the pet a box holds, to be named</summary>
[Message(ServerMessages.OpenPetPackageRequestedMessageComposer)]
public sealed partial record OpenPetPackageRequestedMessageComposer(int ItemId, PetFigureWireData Figure);
