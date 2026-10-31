using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RewardTrack;

/// <summary>RewardTrackProgress (September 2026) — a task's new count, and the player's points on its track. The client
/// tells them of the points a task level gave, from the points before.</summary>
[Message(ServerMessages.RewardTrackProgressComposer)]
public sealed partial record RewardTrackProgressComposer(
    string TrackId,
    string TaskId,
    int ProgressCount,
    int Points);
