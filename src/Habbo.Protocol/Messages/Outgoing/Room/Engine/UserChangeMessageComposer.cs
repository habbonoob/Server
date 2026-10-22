using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3436 UserChangeMessageComposer — a user in the room changed their look or motto. September 2026 reads a
/// string and a list of three numbers each after the score and throws both away, then the user's badge rank:
/// the server has nothing to put in either, so the string is empty and the list's count 0.</summary>
[Message(ServerMessages.UserChangeMessageComposer)]
public sealed partial record UserChangeMessageComposer(
    int Id,
    string Figure,
    string Sex,
    string CustomInfo,
    int AchievementScore,
    [property: WireSince(ClientReleases.September2026)] string Unread = "",
    [property: WireSince(ClientReleases.September2026)] int UnreadTripleCount = 0,
    [property: WireSince(ClientReleases.September2026)] int BadgesRank = -1);
