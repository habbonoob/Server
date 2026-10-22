using Habbo.Protocol.Messages.Incoming.Room.Bots;

namespace Habbo.Gateway.Messages.Handlers.Room.Bots;

/// <summary>CommandBotEvent — use a bot's skill.</summary>
internal sealed class CommandBotHandler : IMessageHandler<CommandBotEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CommandBotEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CommandBotAsync(player.Id, message.BotId, message.SkillId, message.Data);
        return true;
    }
}
