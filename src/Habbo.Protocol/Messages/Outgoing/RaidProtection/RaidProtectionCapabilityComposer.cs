using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RaidProtection;

/// <summary>RaidProtectionCapability (September 2026) — whether the entrant may manage the room's raid protection,
/// sent at the end of every room entry.</summary>
[Message(ServerMessages.RaidProtectionCapabilityComposer)]
public sealed partial record RaidProtectionCapabilityComposer(int RoomId, bool CanManage);
