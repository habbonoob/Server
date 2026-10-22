using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>MyRecommendedRoomsMessageEvent</summary>
[Incoming(ClientMessages.MyRecommendedRoomsMessageEvent)]
public sealed partial record MyRecommendedRoomsMessageEvent;
