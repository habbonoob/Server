using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>3271 SetIgnoreRoomInvites.</summary>
[Incoming(ClientMessages.SetIgnoreRoomInvitesMessageEvent)]
public sealed partial record SetIgnoreRoomInvitesMessageEvent(bool Ignore);
