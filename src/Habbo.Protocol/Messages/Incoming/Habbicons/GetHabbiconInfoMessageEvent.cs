using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>GetHabbiconInfoMessageEvent (September 2026) — one Habbicon, as the player stands with it.</summary>
[Incoming(ClientMessages.GetHabbiconInfoMessageEvent)]
public sealed partial record GetHabbiconInfoMessageEvent(int HabbiconId);
