using Habbo.Silo.Abstractions.Hotel;
using Orleans.Runtime;

namespace Habbo.Silo.Grains.Hotel;

/// <summary>The gateways and silos that registered, each until it stops renewing: one that went away without saying so
/// is forgotten after <see cref="IGatewayDirectoryGrain.Expiry"/>.</summary>
[KeepAlive]
public sealed class GatewayDirectoryGrain : Grain, IGatewayDirectoryGrain
{
    private readonly Dictionary<Guid, (IGatewayClient Client, DateTimeOffset RenewedAt)> _gateways = [];
    private readonly Dictionary<Guid, (IReloadListener Silo, DateTimeOffset RenewedAt)> _silos = [];

    public Task<bool> RegisterAsync(Guid gatewayId, IGatewayClient client)
    {
        var known = _gateways.ContainsKey(gatewayId);
        _gateways[gatewayId] = (client, DateTimeOffset.UtcNow);
        return Task.FromResult(known);
    }

    public Task RegisterSiloAsync(Guid siloId, IReloadListener silo)
    {
        _silos[siloId] = (silo, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(Guid id)
    {
        _gateways.Remove(id);
        _silos.Remove(id);
        return Task.CompletedTask;
    }

    public Task<List<IGatewayClient>> GetGatewaysAsync()
    {
        Prune(_gateways);
        return Task.FromResult(_gateways.Values.Select(g => g.Client).ToList());
    }

    public Task<Dictionary<Guid, IReloadListener>> GetSilosAsync()
    {
        Prune(_silos);
        return Task.FromResult(_silos.ToDictionary(s => s.Key, s => s.Value.Silo));
    }

    private static void Prune<T>(Dictionary<Guid, (T, DateTimeOffset RenewedAt)> registered)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var expired in registered.Where(r => now - r.Value.RenewedAt > IGatewayDirectoryGrain.Expiry).Select(r => r.Key).ToList())
            registered.Remove(expired);
    }
}
