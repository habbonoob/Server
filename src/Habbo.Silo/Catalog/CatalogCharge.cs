using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Silo.Catalog;

/// <summary>The price a purchase took, or why it could not: the credits and points taken, the points
/// left of the kind, and the limited edition's number when it was one.</summary>
internal sealed record CatalogCharge(PurchaseFailure Failure, int Credits, int Points, int? PointsLeft, int? Serial)
{
    public static CatalogCharge Refused(PurchaseFailure failure) => new(failure, 0, 0, null, null);
}
