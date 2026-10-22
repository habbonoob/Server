using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.AvatarEffect;

/// <summary>103 AvatarEffectActivated — start one of the effects waiting in the inventory.</summary>
[Incoming(ClientMessages.AvatarEffectActivatedEvent)]
public sealed partial record AvatarEffectActivatedEvent(int Type);
