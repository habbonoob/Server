using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Advertisement;

/// <summary>InterstitialShownMessageEvent — an ad was shown to the end</summary>
[Incoming(ClientMessages.InterstitialShownMessageEvent)]
public sealed partial record InterstitialShownMessageEvent;
