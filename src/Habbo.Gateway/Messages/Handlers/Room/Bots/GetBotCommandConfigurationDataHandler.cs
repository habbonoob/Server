using Habbo.Protocol.Messages.Incoming.Room.Bots;

namespace Habbo.Gateway.Messages.Handlers.Room.Bots;

/// <summary>GetBotCommandConfigurationDataEvent — how a bot's skill is set up.</summary>
internal sealed class GetBotCommandConfigurationDataHandler : IMessageHandler<GetBotCommandConfigurationDataEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBotCommandConfigurationDataEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SendBotConfigurationAsync(player.Id, message.BotId, message.SkillId);
        return true;
    }
}
