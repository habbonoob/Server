namespace Habbo.Silo.Wired.Variables;

/// <summary>Sub-variables: read-only parts of a variable, named "variable.part", worked out from
/// its value and times for whoever holds it.</summary>
public static class WiredSubVariables
{
    public const char Separator = '.';

    public static WiredVariable Of(WiredVariable parent, string name, Func<int?, WiredVariableValue?, int?> compute) =>
        parent with
        {
            Id = parent.Id + Separator + name,
            Kind = WiredVariableKinds.SubVariable,
            Name = parent.Name + Separator + name,
            HasValue = true,
            CanWriteValue = false,
            CanCreateAndDelete = false,
            CanInterceptChanges = false,
            CanReadCreationTime = false,
            CanReadLastUpdateTime = false,
            TextConnector = new Dictionary<int, string>(),
            SubOf = parent.Id,
            Compute = compute,
        };
}
