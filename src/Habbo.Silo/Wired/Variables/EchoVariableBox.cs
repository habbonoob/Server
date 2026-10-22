using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Variables;

/// <summary>A variable that echoes another — an internal one, say — under a name of its own: reading
/// and writing it reads and writes the one it echoes.</summary>
[WiredBox("wf_var_echo")]
public sealed class EchoVariableBox : WiredVariableBox
{
    public override int Code => VariableCodes.EchoVariable;

    /// <summary>An echo takes the target of what it echoes; alone it belongs to nothing.</summary>
    public override int Target => WiredVariableTargets.Context;

    public override WiredVariable Define(int boxId, FurniWired settings) =>
        Created(boxId, settings, WiredVariableAvailability.WhileRoomActive, hasValue: false, alwaysAvailable: false);

    public override WiredVariable Define(WiredBox box, Func<string, WiredVariable?> find)
    {
        if (box.Settings.VariableIds.FirstOrDefault() is not { Length: > 0 } sourceId || find(sourceId) is not { } source
            || source.EchoOf is not null)
            return Define(box);

        return source with
        {
            Id = box.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Kind = WiredVariableKinds.CreatedByUser,
            Name = box.Settings.StringParam,
            EchoOf = source.Id,
        };
    }
}
