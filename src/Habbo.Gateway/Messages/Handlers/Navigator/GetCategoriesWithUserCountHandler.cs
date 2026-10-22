using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>GetCategoriesWithUserCountMessageEvent — how full each room category is.</summary>
internal sealed class GetCategoriesWithUserCountHandler(IRoomDirectoryService service) : IMessageHandler<GetCategoriesWithUserCountMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCategoriesWithUserCountMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendCategoriesWithUserCountAsync(player);
        return true;
    }
}
