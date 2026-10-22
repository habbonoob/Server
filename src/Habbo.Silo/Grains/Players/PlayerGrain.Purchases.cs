using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Catalog;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Grains.Players;

/// <summary>Buying from the catalog: the price taken in credits and activity points together, and
/// every product handed over — furniture, badges, effects, chat styles — in one transaction. A
/// limited edition sells one at a time, each with the next number of its run.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>A limited edition is bought one at a time.</summary>
    private const int LimitedAmount = 1;

    /// <summary>The most of one offer a single purchase takes, whatever the offer allows: the client's
    /// spinner stops at 100, and a bound keeps price times amount and the grant loops small.</summary>
    private const int MostPerPurchase = 100;

    public async Task<PurchaseResult> PurchaseAsync(ShopOffer offer, int amount, string extraData)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == PlayerId) is not { } user)
            return PurchaseResult.Failed(PurchaseFailure.UnknownOffer);
        if (Refuse(offer, amount, user) is { } refused)
            return refused;
        if (!await PetOrderAcceptedAsync(db, offer, extraData) || !await FurniOrderAcceptedAsync(db, offer, extraData))
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        // Debit and grant together: a crash between the two would otherwise either charge for
        // nothing or hand out free furniture.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var charge = await ChargeAsync(db, user, offer, amount, extraCredits: 0);
        if (charge.Failure != PurchaseFailure.None)
            return PurchaseResult.Failed(charge.Failure) with { ActivityPointType = offer.ActivityPointType };

        var granted = await GrantProductsAsync(db, offer, amount, extraData, user, charge.Serial);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await AnnounceChargeAsync(user, offer, charge);
        await AnnounceGrantAsync(granted);
        await CountTowardsBonusRareAsync(charge.Credits);
        if (offer.LimitedStack > 0)
            await ProgressAchievementAsync(Achievements.AchievementNames.LtdPurchaser, amount);
        if (offer.Products.Where(product => product.Type == ProductTypes.Pet).Sum(product => product.Amount) * amount is > 0 and var pets)
            await ProgressAchievementAsync(Achievements.AchievementNames.PetLover, pets);
        if (offer.Products.Any(product => product.Type == ProductTypes.Club))
        {
            await using var after = await dbFactory.CreateDbContextAsync();
            await ClubAchievementsAsync(after);
        }

        if (offer.Products.Any(product => product.Type == ProductTypes.ChatStyle))
        {
            await using var after = await dbFactory.CreateDbContextAsync();
            await CountChatStylesAsync(after);
        }

        logger.LogInformation(
            "Player {PlayerId} bought offer {Offer} x{Amount} for {Credits} credits and {Points} points; {Granted} item(s) granted.",
            PlayerId, offer.OfferId, amount, charge.Credits, charge.Points, granted.Items);

        return new PurchaseResult(PurchaseFailure.None, offer, user.Credits, granted.Items, ItemIds: granted.Created.Select(i => i.Id).ToList());
    }

    /// <summary>What stops a purchase before anything is taken: a bad amount, silver, nothing in it,
    /// a club gift (which is chosen, not bought), or a club offer for someone outside the club.</summary>
    private static PurchaseResult? Refuse(ShopOffer offer, int amount, User user)
    {
        // The spinner is client-supplied, so it is clamped rather than trusted.
        if (amount < 1 || amount > offer.MaxAmount || amount > MostPerPurchase || (offer.IsLimited && amount != LimitedAmount))
            return PurchaseResult.Failed(PurchaseFailure.InvalidAmount);
        // a pet, a badge or a chat style is handed over once whatever the amount, so it is not charged for more than once
        if (amount > 1 && offer.Products.Any(p => p.Type is ProductTypes.Pet or ProductTypes.Badge or ProductTypes.ChatStyle))
            return PurchaseResult.Failed(PurchaseFailure.InvalidAmount);
        if (offer.NeedsSilver)
            return PurchaseResult.Failed(PurchaseFailure.UnsupportedCurrency);
        if (offer.Products.Count == 0)
            return PurchaseResult.Failed(PurchaseFailure.NothingToGrant);
        if (!offer.IsOnSale || offer.IsClubGift)
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);
        if (user.ClubLevel < offer.ClubLevel)
            return PurchaseResult.Failed(PurchaseFailure.ClubRequired);
        return null;
    }

    /// <summary>Whether what came with a pet purchase names a pet the shop sells: a palette it sells for
    /// that kind, a colour, and a name that would be approved. An offer without a pet needs nothing.</summary>
    private async Task<bool> PetOrderAcceptedAsync(Habbo.Infrastructure.AppDbContext db, ShopOffer offer, string extraData)
    {
        foreach (var product in offer.Products.Where(p => p.Type == ProductTypes.Pet))
        {
            if (Habbo.Silo.Pets.PetOrder.Parse(extraData) is not { } order
                || !await db.PetPalettes.AnyAsync(p => p.TypeId == product.ClassId && p.PaletteId == order.PaletteId && p.Sellable)
                || (await petNames.CheckAsync(order.Name)).Result != Habbo.Protocol.Messages.Outgoing.Users.ApproveNameMessageComposer.Accepted)
                return false;
        }

        return true;
    }

    /// <summary>Whether each piece of furniture in the offer takes what came with the purchase, such as a badge
    /// display case the badge it is to show.</summary>
    private async Task<bool> FurniOrderAcceptedAsync(Habbo.Infrastructure.AppDbContext db, ShopOffer offer, string extraData)
    {
        var definitionIds = offer.Products.Where(p => p.Type == ProductTypes.Furniture).Select(p => p.DefinitionId).OfType<int>().Distinct().ToList();
        foreach (var definition in await db.ItemDefinitions.AsNoTracking().Where(d => definitionIds.Contains(d.Id)).ToListAsync())
        {
            if (!await interactions.For(definition).AcceptsOrderAsync(db, PlayerId, extraData))
                return false;
        }

        return true;
    }

    /// <summary>Takes the price, in credits (plus any extra, such as a gift's wrapping) and activity
    /// points, and the next number of a limited run. Runs inside the purchase's transaction.</summary>
    private async Task<CatalogCharge> ChargeAsync(Habbo.Infrastructure.AppDbContext db, User user, ShopOffer offer, int amount, int extraCredits)
    {
        // worked out in long, so no price times amount wraps round into a refund
        var totalCredits = (long)offer.PriceInCredits * amount + extraCredits;
        var totalPoints = (long)offer.PriceInActivityPoints * amount;
        if (totalCredits is < 0 or > int.MaxValue || totalPoints is < 0 or > int.MaxValue)
            return CatalogCharge.Refused(PurchaseFailure.InvalidAmount);
        var credits = (int)totalCredits;
        var points = (int)totalPoints;
        if (user.Credits < credits)
            return CatalogCharge.Refused(PurchaseFailure.NotEnoughCredits);
        var currency = points > 0 ? await CurrencyAsync(db, offer.ActivityPointType) : null;
        if (currency is not null && currency.Amount < points)
            return CatalogCharge.Refused(PurchaseFailure.NotEnoughActivityPoints);

        // the next number of a limited run is taken first, so two buyers never share one
        int? serial = null;
        if (offer.IsLimited)
        {
            var taken = await db.CatalogOffers
                .Where(o => o.Id == offer.OfferId && o.LimitedSold < o.LimitedStack)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.LimitedSold, o => o.LimitedSold + 1));
            if (taken == 0)
                return CatalogCharge.Refused(PurchaseFailure.SoldOut);
            serial = await db.CatalogOffers.Where(o => o.Id == offer.OfferId).Select(o => o.LimitedSold).FirstAsync();
        }

        // the points are taken in the database, not written back as a total, as SpendActivityPointsAsync takes them
        int? pointsLeft = null;
        if (currency is not null)
        {
            await SaveIfNewAsync(db, currency);
            if (await Balance(db, offer.ActivityPointType).Where(c => c.Amount >= points)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Amount, c => c.Amount - points)) == 0)
                return CatalogCharge.Refused(PurchaseFailure.NotEnoughActivityPoints);
            RecordTransaction(db, offer.ActivityPointType, -points, CurrencyTransactionReasons.CatalogPurchase);
            pointsLeft = await AmountAsync(db, offer.ActivityPointType);
        }

        user.Credits -= credits;
        if (credits > 0)
            RecordTransaction(db, CurrencyTransaction.Credits, -credits, CurrencyTransactionReasons.CatalogPurchase);

        return new CatalogCharge(PurchaseFailure.None, credits, points, pointsLeft, serial);
    }

    /// <summary>Tells the buyer their new balances once the purchase is saved.</summary>
    private Task AnnounceChargeAsync(User user, ShopOffer offer, CatalogCharge charge)
    {
        // The cached view carries the balance the hotel view already rendered.
        Data = Data is null ? null : Data with { Credits = user.Credits };
        if (charge.PointsLeft is { } left)
            Send(new HabboActivityPointNotificationMessageComposer(left, -charge.Points, offer.ActivityPointType));
        return Task.CompletedTask;
    }

    /// <summary>Tells the player what they were handed besides furniture: badges, effects, chat styles.</summary>
    private async Task AnnounceGrantAsync(GrantedProducts granted, bool asGift = false)
    {
        foreach (var bot in granted.Bots)
        {
            var data = Habbo.Protocol.Messages.Outgoing.Inventory.Bots.BotData.From(bot);
            Send(new Habbo.Protocol.Messages.Outgoing.Inventory.Bots.BotAddedToInventoryComposer(data, OpenInventory: true));
            Send(new Habbo.Protocol.Messages.Outgoing.Inventory.Bots.BotReceivedMessageComposer(asGift, data));
        }

        if (granted.Bots.Count > 0)
            Send(UnseenItemsComposer.Bots(granted.Bots.Select(b => b.Id).ToList()));

        foreach (var pet in granted.Pets)
        {
            var data = Habbo.Protocol.Messages.Outgoing.Inventory.Pets.PetData.From(pet);
            Send(new Habbo.Protocol.Messages.Outgoing.Inventory.Pets.PetAddedToInventoryComposer(data, OpenInventory: true));
            Send(new Habbo.Protocol.Messages.Outgoing.Inventory.Pets.PetReceivedMessageComposer(asGift, data));
        }

        if (granted.Pets.Count > 0)
            Send(UnseenItemsComposer.Pets(granted.Pets.Select(p => p.Id).ToList()));

        if (granted.Badges.Count > 0)
        {
            await using var badgeDb = await dbFactory.CreateDbContextAsync();
            foreach (var badge in granted.Badges)
            {
                await SendBadgeReceivedAsync(badgeDb, badge.Id, badge.Code);
                Send(UnseenItemsComposer.Badges([badge.Id]));
            }
        }

        foreach (var (effectId, duration) in granted.Effects)
            await AddEffectAsync(effectId, duration, isPermanent: duration == 0);
        if (granted.ChatStyles > 0)
            await SendChatStylesAsync();
        if (granted.ClubLevel is { } level)
            await AnnounceMembershipAsync(level);
    }

    private async Task<GrantedProducts> GrantProductsAsync(
        Habbo.Infrastructure.AppDbContext db, ShopOffer offer, int amount, string extraData, User user, int? serial, int? seriesSize = null)
    {
        var items = 0;
        var created = new List<UserItem>();
        var badges = new List<UserBadge>();
        var effects = new List<(int, int)>();
        var chatStyles = 0;
        ClubLevel? clubLevel = null;
        var bots = new List<Habbo.Domain.Bots.Bot>();
        var pets = new List<Habbo.Domain.Pets.Pet>();

        var definitionIds = offer.Products.Select(p => p.DefinitionId).OfType<int>().Distinct().ToList();
        var definitions = await db.ItemDefinitions.Where(d => definitionIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id);
        var now = DateTime.UtcNow;

        foreach (var product in offer.Products)
        {
            switch (product.Type)
            {
                case ProductTypes.Badge:
                    if (!await db.UserBadges.AnyAsync(b => b.UserId == PlayerId && b.Code == product.ExtraParam)
                        && badges.All(b => b.Code != product.ExtraParam))
                    {
                        var badge = new UserBadge { UserId = PlayerId, Code = product.ExtraParam };
                        db.UserBadges.Add(badge);
                        badges.Add(badge);
                    }

                    break;

                case ProductTypes.Effect when product.ClassId is { } effectId:
                    for (var i = 0; i < product.Amount * amount; i++)
                        effects.Add((effectId, product.DurationSeconds));
                    break;

                case ProductTypes.ChatStyle when product.ClassId is { } styleId:
                    if (!await db.UserChatStyles.AnyAsync(s => s.UserId == PlayerId && s.StyleId == styleId))
                    {
                        db.UserChatStyles.Add(new UserChatStyle { UserId = PlayerId, StyleId = styleId });
                        chatStyles++;
                    }

                    break;

                case ProductTypes.Club when product.ClassId is { } level:
                    await ExtendMembershipAsync(db, user, (ClubLevel)level,
                        TimeSpan.FromDays(product.Amount * amount * clubOptions.Value.DaysPerMonth) + TimeSpan.FromSeconds(product.DurationSeconds * amount),
                        product.Amount * amount);
                    clubLevel = user.ClubLevel;
                    break;

                case ProductTypes.Bot when product.ClassId is { } typeId
                                           && await db.BotTypes.FirstOrDefaultAsync(t => t.Id == typeId) is { } botType:
                    // the kind names and dresses it as it arrives; the product's look is the one the shop showed
                    for (var i = 0; i < product.Amount * amount; i++)
                    {
                        var bot = new Habbo.Domain.Bots.Bot
                        {
                            OwnerId = PlayerId, TypeId = botType.Id, Name = botType.DefaultName, Motto = botType.DefaultMotto,
                            Figure = product.ExtraParam, Gender = botType.Gender, CreatedAt = DateTimeOffset.UtcNow,
                        };
                        db.Bots.Add(bot);
                        bots.Add(bot);
                    }

                    break;

                case ProductTypes.Pet when product.ClassId is { } typeId && Habbo.Silo.Pets.PetOrder.Parse(extraData) is { } order:
                    var pet = new Habbo.Domain.Pets.Pet
                    {
                        OwnerId = PlayerId, TypeId = typeId, PaletteId = order.PaletteId, Color = order.Color, Name = order.Name,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };
                    petRules.Start(pet, pet.CreatedAt);
                    db.Pets.Add(pet);
                    pets.Add(pet);
                    break;

                case ProductTypes.Furniture when product.DefinitionId is { } definitionId:
                    for (var i = 0; i < product.Amount * amount; i++)
                    {
                        var definition = definitions[definitionId];
                        var item = new UserItem { OwnerId = PlayerId, DefinitionId = definitionId };
                        db.UserItems.Add(item);

                        // What the item starts with is up to its behaviour: usually the product's own
                        // parameter, but a trophy is engraved with what the buyer typed.
                        var grant = new FurniGrant(db, item, definition, product.ExtraParam, extraData, user.Username, now);
                        interactions.For(definition).Grant(grant);

                        // a limited edition is numbered, which no behaviour's data may override
                        if (serial is not null)
                        {
                            item.UniqueSerialNumber = serial;
                            item.UniqueSeriesSize = seriesSize ?? offer.LimitedStack;
                        }

                        // A behaviour may hand over more than the item the product named: buying a
                        // teleport buys the link, which is two doors.
                        items += grant.Items.Count;
                        created.AddRange(grant.Items);
                    }

                    break;
            }
        }

        return new GrantedProducts(items, created, badges, effects, chatStyles, clubLevel, bots, pets);
    }
}
