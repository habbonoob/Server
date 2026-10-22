using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GetQuizQuestionsEvent — a quiz's questions.</summary>
internal sealed class GetQuizQuestionsHandler(IQuizService service) : IMessageHandler<GetQuizQuestionsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetQuizQuestionsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendQuestionsAsync(player, message.QuizCode);
        return true;
    }
}
