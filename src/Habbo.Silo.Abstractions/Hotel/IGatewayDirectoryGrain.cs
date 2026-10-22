using Orleans;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>The hotel's processes that live apart from each other: the gateways the silos send to, and the silos, which
/// tell each other what :update read again. Each registers itself, and again before it is forgotten.</summary>
public interface IGatewayDirectoryGrain : IGrainWithIntegerKey
{
    /// <summary>The one directory.</summary>
    const long Key = 0;

    /// <summary>A gateway forgotten unless it registers again within this.</summary>
    static readonly TimeSpan Expiry = TimeSpan.FromSeconds(30);

    /// <summary>Registers the gateway, or renews it; whether it was known already. A gateway that renews and wasn't
    /// known is talking to a directory that started again: the silo restarted, and with it every grain its players had.</summary>
    Task<bool> RegisterAsync(Guid gatewayId, IGatewayClient client);

    /// <summary>Registers a silo, or renews it.</summary>
    Task RegisterSiloAsync(Guid siloId, IReloadListener silo);

    /// <summary>Forgets a gateway or a silo.</summary>
    Task UnregisterAsync(Guid id);

    Task<List<IGatewayClient>> GetGatewaysAsync();

    /// <summary>The silos, by the id each registered with.</summary>
    Task<Dictionary<Guid, IReloadListener>> GetSilosAsync();
}
