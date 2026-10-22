using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildCreationInfoMessageComposer</summary>
[Message(ServerMessages.GuildCreationInfoMessageComposer)]
public sealed partial record GuildCreationInfoMessageComposer(int CostInCredits, IReadOnlyList<RoomEntryData> OwnedRooms, IReadOnlyList<GuildBadgeSettingsData> BadgeSettings);
