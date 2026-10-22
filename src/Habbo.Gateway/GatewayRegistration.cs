using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Habbo.Gateway;

/// <summary>A gateway in its own process: it tells the silo how to reach its sessions (an observer, registered with
/// the gateway directory and renewed well within its expiry), and hands what arrives that way to its own packet sink.
/// Starting waits until the silo has it, asking again every second, so the game port opens only once the silo can
/// reach the players it takes. A renewal the directory didn't expect means the silo started again without the grains
/// behind this gateway's players: their sessions are closed, so their clients log in again to fresh ones.</summary>
public sealed class GatewayRegistration(
    IGrainFactory grains,
    IClientPacketSink sink,
    SessionManager sessions,
    IEnumerable<IReloadable> reloadables,
    ILogger<GatewayRegistration> logger)
    : IHostedService, IGatewayClient, IDisposable
{
    private static readonly TimeSpan RetryEvery = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RenewEvery = TimeSpan.FromSeconds(5);

    private readonly Guid _gatewayId = Guid.NewGuid();
    private IGatewayClient? _reference;
    private readonly CancellationTokenSource _stopping = new();
    private Task? _renewal;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _reference = grains.CreateObjectReference<IGatewayClient>(this);
        while (await TryRegisterAsync(firstTime: true) is null)
            await Task.Delay(RetryEvery, cancellationToken);
        _renewal = RenewEveryAsync(_stopping.Token);
        logger.LogInformation("Gateway {Gateway} registered with the silo", _gatewayId);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // a renewal under way finishes first, so it can't register the gateway again after it is unregistered
        await _stopping.CancelAsync();
        if (_renewal is not null)
            await _renewal;
        try
        {
            await grains.GetGrain<IGatewayDirectoryGrain>(IGatewayDirectoryGrain.Key).UnregisterAsync(_gatewayId);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not unregister the gateway; it expires on its own");
        }
        if (_reference is not null)
            grains.DeleteObjectReference<IGatewayClient>(_reference);
    }

    /// <summary>Renews the registration until the gateway stops, one renewal at a time.</summary>
    private async Task RenewEveryAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(RenewEvery);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await RenewAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RenewAsync()
    {
        if (await TryRegisterAsync(firstTime: false) is not false)
            return;
        var open = sessions.All;
        logger.LogWarning("The silo started again; closing {Count} session(s) so their clients log in again", open.Count);
        foreach (var session in open)
            session.Close();
    }

    /// <summary>Whether the directory knew the gateway already, or null when the silo couldn't be reached; while
    /// starting, a silo not up yet is only worth a debug line.</summary>
    private async Task<bool?> TryRegisterAsync(bool firstTime)
    {
        try
        {
            return await grains.GetGrain<IGatewayDirectoryGrain>(IGatewayDirectoryGrain.Key).RegisterAsync(_gatewayId, _reference!);
        }
        catch (Exception ex)
        {
            logger.Log(firstTime ? LogLevel.Debug : LogLevel.Warning, ex, "Could not register the gateway with the silo");
            return null;
        }
    }

    public Task Send(Guid sessionGuid, ComposedMessage message)
    {
        sink.TrySend(sessionGuid, message);
        return Task.CompletedTask;
    }

    public Task RefreshPlayer(Guid sessionGuid, PlayerData player)
    {
        sink.RefreshPlayer(sessionGuid, player);
        return Task.CompletedTask;
    }

    public Task Disconnect(Guid sessionGuid)
    {
        sink.Disconnect(sessionGuid);
        return Task.CompletedTask;
    }

    public Task RoomHostLost(SiloAddress silo)
    {
        sink.RoomHostLost(silo);
        return Task.CompletedTask;
    }

    public async Task Reload(string name)
    {
        foreach (var reloadable in reloadables.Where(r => r.ReloadName == name))
            await reloadable.ReloadAsync();
        logger.LogInformation("{Name} read again, as the silo did", name);
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
    }
}
