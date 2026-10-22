using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

/// <summary>800 AvatarEffectExpiredMessageComposer.</summary>
[Message(ServerMessages.AvatarEffectExpiredMessageComposer)]
public sealed partial record AvatarEffectExpiredMessageComposer(int Type);
