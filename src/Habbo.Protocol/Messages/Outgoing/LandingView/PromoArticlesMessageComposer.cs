using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.LandingView;

/// <summary>PromoArticlesMessageComposer — the landing view's promo articles</summary>
[Message(ServerMessages.PromoArticlesMessageComposer)]
public sealed partial record PromoArticlesMessageComposer(IReadOnlyList<PromoArticleData> Articles);
