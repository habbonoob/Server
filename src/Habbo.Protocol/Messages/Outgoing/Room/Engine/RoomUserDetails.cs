using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>The trailing fields a user entity carries. Bots and pets stop before these,
/// so the whole group is optional on the entity.</summary>
[WireStruct]
public sealed partial record RoomUserDetails(
    string Sex,
    int GroupId,
    int GroupStatus,
    string GroupName,
    string SwimFigure,
    int AchievementScore,
    bool IsModerator,
    [property: WireSince(ClientReleases.September2026)] int BadgesRank);
