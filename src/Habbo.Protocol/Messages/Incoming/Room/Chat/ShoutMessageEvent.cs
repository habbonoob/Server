using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Chat;

/// <summary>1400 Shout — the same payload as chat.</summary>
[Incoming(ClientMessages.ShoutMessageEvent)]
public sealed partial record ShoutMessageEvent(string Text, int StyleId, int? TrackingId);
