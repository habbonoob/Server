using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>2178 AvatarEffectMessageComposer — an avatar starts wearing an effect (0: none), after the delay.</summary>
[Message(ServerMessages.AvatarEffectMessageComposer)]
public sealed partial record AvatarEffectMessageComposer(int RoomIndex, int EffectId, int DelayMilliseconds);
