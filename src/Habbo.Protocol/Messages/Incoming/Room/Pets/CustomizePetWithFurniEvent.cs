using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Pets;

/// <summary>CustomizePetWithFurniEvent — use a dye, a hairstyle or a saddle on a pet</summary>
[Incoming(ClientMessages.CustomizePetWithFurniEvent)]
public sealed partial record CustomizePetWithFurniEvent(int ItemId, int PetId);
