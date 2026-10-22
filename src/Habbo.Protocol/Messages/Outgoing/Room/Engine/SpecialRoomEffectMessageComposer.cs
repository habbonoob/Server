using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>SpecialRoomEffectMessageComposer</summary>
[Message(ServerMessages.SpecialRoomEffectMessageComposer)]
public sealed partial record SpecialRoomEffectMessageComposer(int EffectId);
