namespace Habbo.Silo.Commands;

/// <summary>The hotel's other processes, told what :update read again so they read their own copy too. There is none
/// when the silo and the gateway share a process.</summary>
public interface IReloadRelay
{
    void Reload(string name);
}
