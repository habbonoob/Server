using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>3038 Dance — 0 stops; 1 is everyone's dance, 2 to 4 the club's.</summary>
[Incoming(ClientMessages.DanceMessageEvent)]
public sealed partial record DanceMessageEvent(int Style);
