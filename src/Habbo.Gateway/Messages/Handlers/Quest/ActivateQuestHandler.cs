using Habbo.Protocol.Messages.Incoming.Quest;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>ActivateQuestMessageEvent — take on a quest from the hotel view, as accepting it does.</summary>
internal sealed class ActivateQuestHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<ActivateQuestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ActivateQuestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await seasonalQuests.AcceptAsync(player, message.QuestId);
        return true;
    }
}
