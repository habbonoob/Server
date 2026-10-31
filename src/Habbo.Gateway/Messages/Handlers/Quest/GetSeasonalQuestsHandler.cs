using Habbo.Protocol.Messages.Incoming.Quest;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>GetSeasonalQuestsMessageEvent — the seasonal campaigns running, for the calendar and the quest list.</summary>
internal sealed class GetSeasonalQuestsHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<GetSeasonalQuestsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSeasonalQuestsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await seasonalQuests.SendSeasonalQuestsAsync(player);
        return true;
    }
}
