using Habbo.Protocol.Messages.Outgoing.Inventory.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>PetRespectNotificationComposer — a pet was respected</summary>
[Message(ServerMessages.PetRespectNotificationComposer)]
public sealed partial record PetRespectNotificationComposer(int Respect, int PetOwnerId, PetData Pet);
