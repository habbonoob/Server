using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Protocol.Messages.Outgoing.Perk;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>PerkAllowancesMessageEvent — the perks, asked for again.</summary>
internal sealed class PerkAllowancesHandler(IPerkService perks) : IMessageHandler<PerkAllowancesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PerkAllowancesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(PerkAllowancesMessageComposer.From(await perks.GetAllowancesAsync(player, ct)), ct);
        return true;
    }
}
