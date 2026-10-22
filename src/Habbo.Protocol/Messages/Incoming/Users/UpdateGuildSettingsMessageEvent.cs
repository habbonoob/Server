using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>UpdateGuildSettingsMessageEvent</summary>
[Incoming(ClientMessages.UpdateGuildSettingsMessageEvent)]
public sealed partial record UpdateGuildSettingsMessageEvent(int GroupId, int GuildType, int RightsLevel);
