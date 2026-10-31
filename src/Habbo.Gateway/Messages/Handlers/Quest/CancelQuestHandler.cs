using Habbo.Protocol.Messages.Incoming.Quest;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>CancelQuestMessageEvent — give up whichever quest is taken on.</summary>
internal sealed class CancelQuestHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<CancelQuestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelQuestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await seasonalQuests.RejectAsync(player, questId: null);
        return true;
    }
}
