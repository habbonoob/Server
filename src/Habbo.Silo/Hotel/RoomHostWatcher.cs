using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Navigator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Habbo.Silo.Hotel;

/// <summary>Watches the cluster for a silo going, and with it the rooms held there: their counts are forgotten, and the
/// sessions in them are sent to the hotel view, while everything held elsewhere (the players, their friends, their
/// purse) carries on. Every silo watches; telling twice changes nothing.</summary>
public sealed class RoomHostWatcher(
    IClusterMembershipService membership,
    ILocalSiloDetails self,
    IGrainFactory grains,
    IClientPacketSink sink,
    ILogger<RoomHostWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // only silos seen up while this one runs: the dead ones still listed from before were dealt with then
        var up = new HashSet<SiloAddress>();
        await foreach (var snapshot in membership.MembershipUpdates.WithCancellation(stoppingToken))
        {
            foreach (var (silo, member) in snapshot.Members)
            {
                if (member.Status == SiloStatus.Active)
                    up.Add(silo);
                else if (member.Status == SiloStatus.Dead && up.Remove(silo) && !silo.Equals(self.SiloAddress))
                    await LostAsync(silo);
            }
        }
    }

    private async Task LostAsync(SiloAddress silo)
    {
        logger.LogWarning("Silo {Silo} is gone; its rooms' visitors go to the hotel view", silo);
        sink.RoomHostLost(silo);
        try
        {
            await grains.GetGrain<IRoomPopulationGrain>(0).ForgetHostAsync(silo);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not forget the room counts of silo {Silo}", silo);
        }
    }
}
