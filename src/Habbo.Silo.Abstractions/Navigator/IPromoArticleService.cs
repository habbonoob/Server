using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>The articles the landing view promotes.</summary>
public interface IPromoArticleService
{
    Task SendAsync(PlayerData player);
}
