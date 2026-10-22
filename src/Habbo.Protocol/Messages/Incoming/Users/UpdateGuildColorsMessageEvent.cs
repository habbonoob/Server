using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>UpdateGuildColorsMessageEvent</summary>
[Incoming(ClientMessages.UpdateGuildColorsMessageEvent)]
public sealed partial record UpdateGuildColorsMessageEvent(int GroupId, int PrimaryColorId, int SecondaryColorId);
