using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetSellablePetPalettesEvent — the palettes a pet offer sells.</summary>
internal sealed class GetSellablePetPalettesHandler(IPetService service) : IMessageHandler<GetSellablePetPalettesEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSellablePetPalettesEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendSellablePalettesAsync(player, message.ProductCode);
        return true;
    }
}
