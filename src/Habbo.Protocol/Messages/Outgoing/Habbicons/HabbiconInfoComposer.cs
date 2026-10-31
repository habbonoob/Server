using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>HabbiconInfo (September 2026) — one Habbicon, as the player stands with it.</summary>
[Message(ServerMessages.HabbiconInfoComposer)]
public sealed partial record HabbiconInfoComposer(HabbiconData Habbicon);
