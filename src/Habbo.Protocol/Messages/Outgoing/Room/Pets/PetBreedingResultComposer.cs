using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetBreedingResultComposer — the seeds two plants gave</summary>
[Message(ServerMessages.PetBreedingResultComposer)]
public sealed partial record PetBreedingResultComposer(PetBreedingResultData Result, PetBreedingResultData OtherResult);
