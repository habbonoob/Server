using System.Collections.Concurrent;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Habbo.Silo.Hotel;

/// <summary>The packet sink of a silo whose gateways run in other processes: each packet goes to every registered
/// gateway, and the one holding the session sends it. Which gateways there are is asked again every second from the
/// silo's start, so the list is ready before the first player arrives; what is sent while there is none (a gateway
/// restarting, its players logging straight back in) waits a few seconds for one rather than being lost. The silo
/// registers itself the same way, so what :update reads again here reaches the other silos as well as the gateways,
/// and what they read again is read here.</summary>
public sealed class GatewayPacketSink(
    IGrainFactory grains,
    IEnumerable<IReloadable> reloadables,
    ILogger<GatewayPacketSink> logger)
    : IClientPacketSink, IReloadRelay, IReloadListener, IHostedService, IDisposable
{
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromSeconds(1);

    /// <summary>How long what is sent waits for a gateway, and how much of it at most.</summary>
    private static readonly TimeSpan HoldFor = TimeSpan.FromSeconds(5);
    private const int MaxHeld = 10_000;

    private readonly Guid _siloId = Guid.NewGuid();
    private IReloadListener? _reference;
    private volatile IReadOnlyList<IGatewayClient> _gateways = [];
    private volatile IReadOnlyList<IReloadListener> _otherSilos = [];
    private readonly ConcurrentQueue<(DateTimeOffset At, Action<IGatewayClient> Call)> _held = new();
    private PeriodicTimer? _timer;
    private bool _asked;
    private Task? _loop;

    public bool TrySend(Guid sessionGuid, ComposedMessage message)
    {
        Deliver(gateway => Forget(gateway.Send(sessionGuid, message)));
        return true;
    }

    public void RefreshPlayer(Guid sessionGuid, PlayerData player) =>
        Deliver(gateway => Forget(gateway.RefreshPlayer(sessionGuid, player)));

    public void Disconnect(Guid sessionGuid) => Deliver(gateway => Forget(gateway.Disconnect(sessionGuid)));

    public void RoomHostLost(SiloAddress silo) => Deliver(gateway => Forget(gateway.RoomHostLost(silo)));

    /// <summary>What :update read again here, read again by every gateway and every other silo.</summary>
    void IReloadRelay.Reload(string name)
    {
        Deliver(gateway => Forget(gateway.Reload(name)));
        foreach (var silo in _otherSilos)
            Forget(silo.Reload(name));
    }

    /// <summary>What :update read again on another silo, read again here.</summary>
    async Task IReloadListener.Reload(string name)
    {
        foreach (var reloadable in reloadables.Where(r => r.ReloadName == name))
            await reloadable.ReloadAsync();
        logger.LogInformation("{Name} read again, as another silo did", name);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _reference = grains.CreateObjectReference<IReloadListener>(this);
        _timer = new PeriodicTimer(RefreshEvery);
        _loop = RefreshLoopAsync(_timer);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        if (_loop is not null)
            await _loop.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        try
        {
            await grains.GetGrain<IGatewayDirectoryGrain>(IGatewayDirectoryGrain.Key).UnregisterAsync(_siloId);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not unregister the silo; it expires on its own");
        }
        if (_reference is not null)
            grains.DeleteObjectReference<IReloadListener>(_reference);
    }

    public void Dispose() => _timer?.Dispose();

    private void Deliver(Action<IGatewayClient> call)
    {
        var gateways = _gateways;
        if (gateways.Count == 0)
        {
            if (_held.Count < MaxHeld)
                _held.Enqueue((DateTimeOffset.UtcNow, call));
            return;
        }
        foreach (var gateway in gateways)
            call(gateway);
    }

    private async Task RefreshLoopAsync(PeriodicTimer timer)
    {
        do
        {
            try
            {
                var directory = grains.GetGrain<IGatewayDirectoryGrain>(IGatewayDirectoryGrain.Key);
                await directory.RegisterSiloAsync(_siloId, _reference!);
                _gateways = await directory.GetGatewaysAsync();
                _otherSilos = (await directory.GetSilosAsync()).Where(s => s.Key != _siloId).Select(s => s.Value).ToList();
                _asked = true;
                ReleaseHeld();
            }
            catch (Exception ex)
            {
                // until the silo has started, the directory can't be reached yet
                logger.Log(_asked ? LogLevel.Warning : LogLevel.Debug, ex, "Could not ask which gateways there are");
            }
        }
        while (await timer.WaitForNextTickAsync().ConfigureAwait(false));
    }

    /// <summary>What waited goes out once there is a gateway, in the order it was sent; what waited too long is let go.</summary>
    private void ReleaseHeld()
    {
        var gateways = _gateways;
        var now = DateTimeOffset.UtcNow;
        while (_held.TryPeek(out var held))
        {
            if (gateways.Count == 0 && now - held.At < HoldFor)
                return;
            _held.TryDequeue(out _);
            if (now - held.At >= HoldFor)
                continue;
            foreach (var gateway in gateways)
                held.Call(gateway);
        }
    }

    /// <summary>A one-way call to a gateway or silo that went away fails on its own; nothing waits on it.</summary>
    private void Forget(Task call) =>
        call.ContinueWith(t => logger.LogDebug(t.Exception, "A gateway did not take a packet"), TaskContinuationOptions.OnlyOnFaulted);
}
