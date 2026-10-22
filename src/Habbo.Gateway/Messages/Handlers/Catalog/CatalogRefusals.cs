using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>What the client is told a refused purchase ran into; each has its own alert.</summary>
internal static class CatalogRefusals
{
    public static IComposer For(PurchaseResult result) => result.Failure switch
    {
        PurchaseFailure.NotEnoughCredits => new NotEnoughBalanceMessageComposer(true, false),
        PurchaseFailure.NotEnoughActivityPoints => new NotEnoughBalanceMessageComposer(false, true, result.ActivityPointType),
        PurchaseFailure.SoldOut => new LimitedEditionSoldOutComposer(),
        PurchaseFailure.ClubRequired => new PurchaseNotAllowedMessageComposer(PurchaseNotAllowedMessageComposer.ClubRequired),
        PurchaseFailure.NotAllowed => new PurchaseNotAllowedMessageComposer(PurchaseNotAllowedMessageComposer.NotAllowed),
        PurchaseFailure.ReceiverNotFound => new GiftReceiverNotFoundComposer(),
        _ => PurchaseErrorMessageComposer.Failed(),
    };
}
