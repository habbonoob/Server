using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.LandingView;

/// <summary>GetPromoArticlesMessageEvent — the landing view's promo articles</summary>
[Incoming(ClientMessages.GetPromoArticlesMessageEvent)]
public sealed partial record GetPromoArticlesMessageEvent;
