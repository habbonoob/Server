using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetHabboGroupDetailsMessageEvent</summary>
[Incoming(ClientMessages.GetHabboGroupDetailsMessageEvent)]
public sealed partial record GetHabboGroupDetailsMessageEvent(int GroupId, bool OpenDetails);
