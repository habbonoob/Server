using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Advertisement;

/// <summary>GetInterstitialMessageEvent — whether an ad may be shown as the player goes into a room</summary>
[Incoming(ClientMessages.GetInterstitialMessageEvent)]
public sealed partial record GetInterstitialMessageEvent;
