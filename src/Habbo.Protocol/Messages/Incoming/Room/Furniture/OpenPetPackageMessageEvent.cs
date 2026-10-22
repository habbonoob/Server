using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>OpenPetPackageMessageEvent — open a box holding a pet, naming the pet</summary>
[Incoming(ClientMessages.OpenPetPackageMessageEvent)]
public sealed partial record OpenPetPackageMessageEvent(int ItemId, string PetName);
