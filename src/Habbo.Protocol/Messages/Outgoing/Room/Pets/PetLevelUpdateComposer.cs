using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetLevelUpdateComposer — a pet went up a level</summary>
[Message(ServerMessages.PetLevelUpdateComposer)]
public sealed partial record PetLevelUpdateComposer(int RoomIndex, int PetId, int Level);
