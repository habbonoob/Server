using Habbo.Silo.Abstractions.Players;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>A gateway, as the silo reaches it from another process: what the packet sink does there for the sessions it
/// holds. A session it doesn't hold is ignored, so the silo may tell every gateway. One way: nothing waits on a client.</summary>
public interface IGatewayClient : IReloadListener
{
    [OneWay]
    Task Send(Guid sessionGuid, ComposedMessage message);

    [OneWay]
    Task RefreshPlayer(Guid sessionGuid, PlayerData player);

    [OneWay]
    Task Disconnect(Guid sessionGuid);

    /// <summary>The silo that held these rooms is gone, and with it everything in them: every session in one of its
    /// rooms is out of it, and its client is sent to the hotel view.</summary>
    [OneWay]
    Task RoomHostLost(SiloAddress silo);
}
