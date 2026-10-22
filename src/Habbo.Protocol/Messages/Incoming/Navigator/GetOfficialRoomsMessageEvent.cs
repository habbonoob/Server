using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>GetOfficialRoomsMessageEvent</summary>
[Incoming(ClientMessages.GetOfficialRoomsMessageEvent)]
public sealed partial record GetOfficialRoomsMessageEvent(int AdIndex);
