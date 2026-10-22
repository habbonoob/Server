using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Advertisement;

/// <summary>InterstitialMessageComposer — whether the client may show an ad now</summary>
[Message(ServerMessages.InterstitialMessageComposer)]
public sealed partial record InterstitialMessageComposer(bool CanShowInterstitial);
