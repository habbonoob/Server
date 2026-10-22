using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RateFlatMessageEvent</summary>
[Incoming(ClientMessages.RateFlatMessageEvent)]
public sealed partial record RateFlatMessageEvent(int Points);
