using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetExperienceComposer — a pet gained experience</summary>
[Message(ServerMessages.PetExperienceComposer)]
public sealed partial record PetExperienceComposer(int PetId, int PetRoomIndex, int GainedExperience);
