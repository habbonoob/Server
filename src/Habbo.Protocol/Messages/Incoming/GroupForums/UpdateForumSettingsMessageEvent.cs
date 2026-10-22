using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>UpdateForumSettingsMessageEvent</summary>
[Incoming(ClientMessages.UpdateForumSettingsMessageEvent)]
public sealed partial record UpdateForumSettingsMessageEvent(int GroupId, int ReadPermission, int PostMessagePermission, int PostThreadPermission, int ModeratePermission);
