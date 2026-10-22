using Orleans;
using Orleans.Concurrency;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>One of the hotel's processes, told what :update read again elsewhere so it reads its own copy too.</summary>
public interface IReloadListener : IGrainObserver
{
    /// <summary>Reads again what goes by this <see cref="IReloadable.ReloadName"/>.</summary>
    [OneWay]
    Task Reload(string name);
}
