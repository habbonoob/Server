using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

[WireStruct]
public sealed partial record ExtendedProfileData(
    int UserId,
    string UserName,
    string Figure,
    string Motto,
    string CreationDate,
    int AchievementScore,
    int FriendCount,
    bool IsFriend,
    bool IsFriendRequestSent,
    [property: WireUntil(ClientReleases.September2026)] bool IsOnline,
    [property: WireSince(ClientReleases.September2026), WireFormat(WireType.U8)] int OnlineStatus,
    IReadOnlyList<HabboGroupEntryData> Guilds,
    int LastAccessSinceInSeconds,
    bool OpenProfileWindow,
    bool IsHidden,
    int AccountLevel,
    int IntegerField,
    int StarGemCount,
    bool BooleanField1,
    bool BooleanField2,
    [property: WireSince(ClientReleases.September2026)] int TotalBadges,
    [property: WireSince(ClientReleases.September2026)] int AchievementLevel,
    [property: WireSince(ClientReleases.September2026)] IReadOnlyList<BadgeRarityCountData> BadgeRarityCounts,
    [property: WireSince(ClientReleases.September2026)] int TotalBadgesRank)
{
    /// <summary>What the client shows as "-": a count or a time the player keeps to themselves.</summary>
    public const int Unknown = -1;

    /// <summary>OnlineStatus (September 2026): what the profile shows beside the name.</summary>
    public const int Offline = 0, Online = 1, OnlineHidden = 2;
}
