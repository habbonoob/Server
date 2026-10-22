using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.AvatarEffect;

/// <summary>704 AvatarEffectAddedMessageComposer.</summary>
[Message(ServerMessages.AvatarEffectAddedMessageComposer)]
public sealed partial record AvatarEffectAddedMessageComposer(int Type, int SubType, int Duration, bool IsPermanent);
