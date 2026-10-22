using Habbo.Domain.Catalog;
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The club's memberships and monthly gifts, as catalog offers on their pages.</summary>
public static partial class DbSeeder
{
    private sealed record SeedClub(
        string MembershipPage, List<SeedMembership>? Memberships, string GiftsPage, List<SeedClubGift>? Gifts);

    private sealed record SeedMembership(
        int Id, string ProductCode, int PriceInCredits, ClubLevel Level, int Months, int ExtraDays, bool Giftable);

    private sealed record SeedClubGift(int Id, string ClassName, int DaysRequired);

    private const int SecondsPerDay = 86400;

    /// <summary>seed/club.json. Offers are matched by id, so a membership or a gift the hotel already
    /// has keeps whatever it was changed to.</summary>
    private static async Task SeedClubAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var club = await seedFiles.ReadJsonAsync<SeedClub>("club.json", ct);
        if (club is null)
            return;

        var known = await db.CatalogOffers.Select(o => o.Id).ToListAsync(ct);
        var pages = await db.CatalogPages.Where(p => p.PageName == club.MembershipPage || p.PageName == club.GiftsPage)
            .ToDictionaryAsync(p => p.PageName, p => p.Id, ct);
        var added = 0;

        if (pages.TryGetValue(club.MembershipPage, out var membershipPage))
        {
            var position = 0;
            foreach (var membership in (club.Memberships ?? []).Where(m => !known.Contains(m.Id)))
            {
                var offer = new CatalogOffer
                {
                    Id = membership.Id, LocalizationId = membership.ProductCode, PriceInCredits = membership.PriceInCredits,
                    Giftable = membership.Giftable, MaxAmount = 1,
                };
                offer.Products.Add(new CatalogProduct
                {
                    Type = CatalogProductType.Club, ClassId = (int)membership.Level, Amount = membership.Months,
                    DurationSeconds = membership.ExtraDays * SecondsPerDay,
                });
                offer.Listings.Add(new CatalogListing { PageId = membershipPage, OrderIndex = position++ });
                db.CatalogOffers.Add(offer);
                added++;
            }
        }

        if (pages.TryGetValue(club.GiftsPage, out var giftsPage))
        {
            var definitions = await db.ItemDefinitions.ToDictionaryAsync(d => d.ClassName, d => d.Id, ct);
            var position = 0;
            foreach (var gift in (club.Gifts ?? []).Where(g => !known.Contains(g.Id)))
            {
                if (!definitions.TryGetValue(gift.ClassName, out var definitionId))
                {
                    logger.LogWarning("club.json names gift furniture {Class}, which is not in furnidata.", gift.ClassName);
                    continue;
                }

                var offer = new CatalogOffer
                {
                    Id = gift.Id, LocalizationId = gift.ClassName, MaxAmount = 1, ClubLevel = ClubLevel.Vip,
                    ClubGiftDaysRequired = gift.DaysRequired,
                };
                offer.Products.Add(new CatalogProduct { Type = CatalogProductType.Furniture, DefinitionId = definitionId });
                offer.Listings.Add(new CatalogListing { PageId = giftsPage, OrderIndex = position++ });
                db.CatalogOffers.Add(offer);
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} club offer(s) from {File}.", added, "club.json");
        }
    }
}
