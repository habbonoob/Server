using Habbo.Infrastructure;

namespace Habbo.Silo.Wired.Variables;

/// <summary>habbo.com's internal and smart variables, read from seed/wired_internal_variables.json once: every one
/// of them exactly as Habbo defines it, with what this server can read and write of it attached.</summary>
public sealed class WiredInternalVariableCatalog(SeedFiles seedFiles)
{
    public const string SeedFile = "wired_internal_variables.json";

    private WiredInternalVariables? _variables;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<WiredInternalVariables> LoadAsync(CancellationToken ct = default)
    {
        if (_variables is not null)
            return _variables;

        await _loading.WaitAsync(ct);
        try
        {
            if (_variables is not null)
                return _variables;

            var file = await seedFiles.ReadJsonAsync<SeedFileData>(SeedFile, ct);
            return _variables = WiredInternalVariables.From((file?.Variables ?? []).Select(Define));
        }
        finally
        {
            _loading.Release();
        }
    }

    private static WiredVariable Define(SeedVariable v) => new(
        v.Id, v.Kind, v.Name, v.Availability, v.Target, v.AlwaysAvailable, v.CanCreateAndDelete, v.HasValue, v.CanWriteValue,
        v.CanInterceptChanges, v.IsInvisible, v.CanReadCreationTime, v.CanReadLastUpdateTime)
    {
        RequiresExecutionContext = v.RequiresExecutionContext,
        // an empty "texts" is a connector all the same: the client spells those values out itself
        AlwaysHasTextConnector = v.Texts is not null,
        TextConnector = v.Texts ?? new Dictionary<int, string>(),
        TextConnectorFallback = v.TextFallback,
    };

    private sealed record SeedFileData(List<SeedVariable>? Variables);

    private sealed record SeedVariable(
        string Id, int Kind, string Name, int Availability, int Target,
        bool AlwaysAvailable, bool CanCreateAndDelete, bool HasValue, bool CanWriteValue, bool CanInterceptChanges,
        bool IsInvisible, bool RequiresExecutionContext, bool CanReadCreationTime, bool CanReadLastUpdateTime,
        Dictionary<int, string>? Texts, string? TextFallback);
}
