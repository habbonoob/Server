using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Wired.Variables;

/// <summary>A variable that stands for one another of the owner's rooms shares (a user or room
/// variable kept and shared across rooms): reading it reads that room's value; writing it, unless
/// the box is read-only, writes there.</summary>
[WiredBox("wf_var_reference")]
public sealed class ReferenceVariableBox : WiredVariableBox
{
    private const int ReadOnly = 0;

    public override int Code => VariableCodes.ReferenceVariable;

    /// <summary>A reference takes the target of what it stands for; alone it belongs to nothing.</summary>
    public override int Target => WiredVariableTargets.Context;

    // read-only, as Habbo places them
    public override IReadOnlyList<int> DefaultIntParams => [1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override WiredVariable Define(int boxId, FurniWired settings) =>
        Created(boxId, settings, WiredVariableAvailability.Reference, hasValue: false, alwaysAvailable: false);

    public override WiredVariable Define(WiredBox box, Func<string, WiredVariable?> find)
    {
        if (box.Settings.VariableIds.FirstOrDefault() is not { Length: > 0 } sourceId
            || find(sourceId) is not { SourceRoomId: not null } source)
            return Define(box);

        var readOnly = box.Flag(ReadOnly);
        return source with
        {
            Id = box.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Kind = WiredVariableKinds.CreatedByUser,
            Name = box.Settings.StringParam,
            Availability = WiredVariableAvailability.Reference,
            CanWriteValue = source.CanWriteValue && !readOnly,
            CanCreateAndDelete = source.CanCreateAndDelete && !readOnly,
            SourceId = source.Id,
        };
    }
}
