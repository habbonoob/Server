using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Achievements;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>PostQuizAnswersEvent — mark a quiz's answers.</summary>
internal sealed class PostQuizAnswersHandler(IQuizService service) : IMessageHandler<PostQuizAnswersEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PostQuizAnswersEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.MarkAnswersAsync(player, message.QuizCode, message.Answers);
        return true;
    }
}
