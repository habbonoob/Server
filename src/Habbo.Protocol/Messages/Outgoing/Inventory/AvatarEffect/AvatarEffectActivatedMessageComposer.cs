using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

/// <summary>2027 AvatarEffectActivatedMessageComposer.</summary>
[Message(ServerMessages.AvatarEffectActivatedMessageComposer)]
public sealed partial record AvatarEffectActivatedMessageComposer(int Type, int Duration, bool IsPermanent);
