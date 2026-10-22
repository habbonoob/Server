using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Incoming.Register;

namespace Habbo.Gateway.Messages.Handlers.Register;

/// <summary>UpdateFigureDataMessageEvent — the avatar editor's save. A look the figure data does not
/// allow is ignored.</summary>
internal sealed class UpdateFigureDataHandler : IMessageHandler<UpdateFigureDataMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateFigureDataMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        if (await playerGrain.UpdateFigureAsync(message.Figure, GenderExtensions.ParseLetter(message.Gender)) is { } player)
            await PlayerRefresh.RefreshAsync(context, player);
        return true;
    }
}
