using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>TogglePetRidingPermissionMessageEvent — let anyone ride the pet, or only its owner</summary>
[Incoming(ClientMessages.TogglePetRidingPermissionMessageEvent)]
public sealed partial record TogglePetRidingPermissionMessageEvent(int PetId);
