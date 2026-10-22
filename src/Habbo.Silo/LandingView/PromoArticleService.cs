using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.LandingView;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.LandingView;

/// <summary>The articles the landing view promotes (seed/promo_articles.json), the active ones in order.</summary>
public sealed class PromoArticleService(IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger) : IPromoArticleService
{
    public async Task SendAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var articles = await db.PromoArticles.AsNoTracking().Where(a => a.Active).OrderBy(a => a.SortOrder).ThenBy(a => a.Id).ToListAsync();
        await messenger.SendAsync(player.Id, new PromoArticlesMessageComposer(articles
            .Select(a => new PromoArticleData(a.Id, a.Title, a.BodyText, a.ButtonText, a.LinkType, a.LinkContent, a.ImageUrl)).ToList()));
    }
}
