using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetReceivedMessageComposer — a pet was bought or given</summary>
[Message(ServerMessages.PetReceivedMessageComposer)]
public sealed partial record PetReceivedMessageComposer(bool BoughtAsGift, PetData Pet);
