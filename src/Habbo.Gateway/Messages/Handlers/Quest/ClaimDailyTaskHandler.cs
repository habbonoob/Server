using Habbo.Protocol.Messages.Incoming.Quest.DailyTasks;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>ClaimDailyTaskEvent — claim a done daily task's reward.</summary>
internal sealed class ClaimDailyTaskHandler(IGrainFactory grains) : IMessageHandler<ClaimDailyTaskEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClaimDailyTaskEvent message, CancellationToken ct)
    {
        if (context.Session.Player is { } player)
            await grains.GetGrain<IPlayerGrain>(player.Id).ClaimDailyTaskAsync(message.TaskId);
        return true;
    }
}
