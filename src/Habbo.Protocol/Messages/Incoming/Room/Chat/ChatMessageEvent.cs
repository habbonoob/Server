using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Chat;

/// <summary>1119 Chat — the tracking id is only sent by newer clients.</summary>
[Incoming(ClientMessages.ChatMessageEvent)]
public sealed partial record ChatMessageEvent(string Text, int StyleId, int? TrackingId);
