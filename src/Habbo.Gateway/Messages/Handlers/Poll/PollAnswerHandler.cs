using Habbo.Protocol.Messages.Incoming.Poll;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Poll;

/// <summary>PollAnswerEvent — answer a poll question.</summary>
internal sealed class PollAnswerHandler(IPollService service) : IMessageHandler<PollAnswerEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PollAnswerEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.AnswerAsync(player, message.PollId, message.QuestionId, message.Answers);
        return true;
    }
}
