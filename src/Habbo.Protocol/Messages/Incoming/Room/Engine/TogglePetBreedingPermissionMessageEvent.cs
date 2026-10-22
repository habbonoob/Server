using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>TogglePetBreedingPermissionMessageEvent — let the pet be bred with others' pets, or not</summary>
[Incoming(ClientMessages.TogglePetBreedingPermissionMessageEvent)]
public sealed partial record TogglePetBreedingPermissionMessageEvent(int PetId);
