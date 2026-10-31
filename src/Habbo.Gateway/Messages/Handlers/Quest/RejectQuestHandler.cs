using Habbo.Protocol.Messages.Incoming.Quest;
using Habbo.Silo.Abstractions.Quests;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>RejectQuestMessageEvent — give up the quest taken on.</summary>
internal sealed class RejectQuestHandler(ISeasonalQuestService seasonalQuests) : IMessageHandler<RejectQuestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RejectQuestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await seasonalQuests.RejectAsync(player, message.QuestId);
        return true;
    }
}
