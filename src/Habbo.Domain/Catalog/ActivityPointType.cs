namespace Habbo.Domain.Catalog;

/// <summary>The non-credit currencies an offer can be priced in. The values are the client's
/// own: it reads the number off the wire and picks the icon and balance to show from it, so
/// these cannot be renumbered.
///
/// Not exhaustive. Habbo adds currencies without telling anyone — a live US dump carried three
/// offers priced in type 101 — so the stored value is whatever the wire said and may not be
/// named here. Treat an unnamed value as a currency we simply do not know yet rather than as
/// bad data; the price is still real, which is what stops it being sold for free.</summary>
public enum ActivityPointType
{
    Ducket = 0,
    Seashell = 1,
    Heart = 2,
    GiftPoint = 3,
    Shell = 4,
    Diamond = 5,
}
