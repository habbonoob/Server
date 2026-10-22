using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>HarvestPetMessageEvent — harvest a grown plant</summary>
[Incoming(ClientMessages.HarvestPetMessageEvent)]
public sealed partial record HarvestPetMessageEvent(int PetId);
