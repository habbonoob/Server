using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

/// <summary>102 AvatarEffectSelectedMessageComposer.</summary>
[Message(ServerMessages.AvatarEffectSelectedMessageComposer)]
public sealed partial record AvatarEffectSelectedMessageComposer(int Type);
