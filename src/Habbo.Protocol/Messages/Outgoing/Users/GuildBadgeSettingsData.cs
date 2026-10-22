using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GuildBadgeSettingsData — one badge layer</summary>
[WireStruct]
public sealed partial record GuildBadgeSettingsData(int PartId, int ColorId, int Position);
