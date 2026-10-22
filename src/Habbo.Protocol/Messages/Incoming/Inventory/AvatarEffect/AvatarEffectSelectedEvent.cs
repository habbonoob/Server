using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.AvatarEffect;

/// <summary>2914 AvatarEffectSelected — wear an active effect; -1 takes it off.</summary>
[Incoming(ClientMessages.AvatarEffectSelectedEvent)]
public sealed partial record AvatarEffectSelectedEvent(int Type);
