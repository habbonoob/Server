using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.LandingView;

/// <summary>PromoArticleData — a promo article and where its button leads</summary>
[WireStruct]
public sealed partial record PromoArticleData(int Id, string Title, string BodyText, string ButtonText, int LinkType, string LinkContent, string ImageUrl);
