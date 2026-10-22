namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>The hotel's client variables (external_variables), for the settings the client and
/// the server share: the wired time zones, say.</summary>
public interface IClientVariables
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
}
