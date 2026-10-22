using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Chat;

/// <summary>802 Whisper — the same payload as chat.</summary>
[Incoming(ClientMessages.WhisperMessageEvent)]
public sealed partial record WhisperMessageEvent(string Text, int StyleId, int? TrackingId);
