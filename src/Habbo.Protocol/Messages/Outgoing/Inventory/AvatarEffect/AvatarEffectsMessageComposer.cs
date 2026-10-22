using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

/// <summary>1450 AvatarEffectsMessageComposer — the effects inventory.</summary>
[Message(ServerMessages.AvatarEffectsMessageComposer)]
public sealed partial record AvatarEffectsMessageComposer(IReadOnlyList<AvatarEffectData> Effects);
