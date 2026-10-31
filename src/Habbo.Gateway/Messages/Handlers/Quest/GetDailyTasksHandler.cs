using Habbo.Protocol.Messages.Incoming.Quest.DailyTasks;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Quest;

/// <summary>GetDailyTasksEvent — the player's daily tasks.</summary>
internal sealed class GetDailyTasksHandler(IGrainFactory grains) : IMessageHandler<GetDailyTasksEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetDailyTasksEvent message, CancellationToken ct)
    {
        if (context.Session.Player is { } player)
            await grains.GetGrain<IPlayerGrain>(player.Id).SendDailyTasksAsync();
        return true;
    }
}
