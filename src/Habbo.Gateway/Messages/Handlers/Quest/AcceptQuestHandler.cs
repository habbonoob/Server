using Habbo.Protocol.Messages.Incoming.Quest;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>AcceptQuestMessageEvent — take on a quest from inside a room. Only seasonal quests are run.</summary>
internal sealed class AcceptQuestHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<AcceptQuestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AcceptQuestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await seasonalQuests.AcceptAsync(player, message.QuestId);
        return true;
    }
}
