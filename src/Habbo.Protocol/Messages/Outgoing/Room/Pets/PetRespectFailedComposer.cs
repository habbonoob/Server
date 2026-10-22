using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetRespectFailedComposer — the player is too new to respect pets</summary>
[Message(ServerMessages.PetRespectFailedComposer)]
public sealed partial record PetRespectFailedComposer(int RequiredDays, int AvatarAgeInDays);
