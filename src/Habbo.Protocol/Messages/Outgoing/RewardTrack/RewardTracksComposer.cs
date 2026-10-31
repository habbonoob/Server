using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RewardTrack;

/// <summary>RewardTracks (September 2026) — every reward track and the player's progress on it, sent at login (Habbo's
/// own, captured: after the figure sets, before the Habbicons). Disabled closes them; Reload tells a player who has a
/// track open that it changed.</summary>
[Message(ServerMessages.RewardTracksComposer)]
public sealed partial record RewardTracksComposer(
    bool Disabled,
    IReadOnlyList<RewardTrackData> Tracks,
    bool Reload);
