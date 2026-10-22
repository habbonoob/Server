using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildEditInfoMessageComposer</summary>
[Message(ServerMessages.GuildEditInfoMessageComposer)]
public sealed partial record GuildEditInfoMessageComposer(IReadOnlyList<RoomEntryData> OwnedRooms, bool IsOwner, int GroupId, string GroupName, string GroupDescription, int BaseRoomId, int PrimaryColorId, int SecondaryColorId, int GuildType, int GuildRightsLevel, bool Locked, string Url, IReadOnlyList<GuildBadgeSettingsData> BadgeSettings, string BadgeCode, int MembershipCount);
