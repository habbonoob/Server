using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RoomsWithHighestScoreSearchMessageEvent</summary>
[Incoming(ClientMessages.RoomsWithHighestScoreSearchMessageEvent)]
public sealed partial record RoomsWithHighestScoreSearchMessageEvent(int AdIndex);
