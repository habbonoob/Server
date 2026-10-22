using Habbo.Domain.Furniture;
using Habbo.Domain.Users;

namespace Habbo.Silo.Catalog;

/// <summary>What a purchase handed over, for what the buyer is told once it is saved: the furniture
/// (counted, and the rows themselves), the new badges, the effects to add, how many chat styles, the
/// club level a membership bought left them at, and the new bots and pets.</summary>
internal sealed record GrantedProducts(
    int Items, List<UserItem> Created, List<UserBadge> Badges, List<(int EffectId, int Duration)> Effects, int ChatStyles,
    ClubLevel? ClubLevel, List<Habbo.Domain.Bots.Bot> Bots, List<Habbo.Domain.Pets.Pet> Pets);
