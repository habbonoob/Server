using System.Text.RegularExpressions;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Wired.Variables;

/// <summary>The variable boxes' dialog codes (the client's VariableCodes).</summary>
public static class VariableCodes
{
    public const int FurniVariable = 0;
    public const int UserVariable = 1;
    public const int GlobalVariable = 2;
    public const int ContextVariable = 3;
    public const int ReferenceVariable = 4;
    public const int QuestVariable = 5;
    public const int QuestChainVariable = 6;
    public const int EchoVariable = 7;
    public const int DailyTaskVariable = 8;
}

/// <summary>A box that creates a variable, named by its string. The box's item id is the variable's id.</summary>
public abstract partial class WiredVariableBox : WiredBoxType
{
    public const int MaxNameLength = 40;

    public override WiredKind Kind => WiredKind.Variable;

    public abstract int Target { get; }

    /// <summary>The variable a box makes, as its settings describe it: the box need not be in
    /// this room — a variable another room shares is described the same way.</summary>
    public abstract WiredVariable Define(int boxId, FurniWired settings);

    public WiredVariable Define(WiredBox box) => Define(box.Id, box.Settings);

    /// <summary>The variable the box makes, for a box whose variable depends on others.</summary>
    public virtual WiredVariable Define(WiredBox box, Func<string, WiredVariable?> find) => Define(box);

    protected static int Param(FurniWired settings, int index, int fallback = 0) =>
        index < settings.IntParams.Count ? settings.IntParams[index] : fallback;

    protected WiredVariable Created(int boxId, FurniWired settings, int availability, bool hasValue, bool alwaysAvailable) => new(
        boxId.ToString(System.Globalization.CultureInfo.InvariantCulture), WiredVariableKinds.CreatedByUser,
        settings.StringParam, availability, Target,
        AlwaysAvailable: alwaysAvailable, CanCreateAndDelete: !alwaysAvailable, HasValue: hasValue, CanWriteValue: hasValue,
        CanInterceptChanges: true, IsInvisible: false, CanReadCreationTime: true, CanReadLastUpdateTime: true);

    public override WiredValidation? Validate(FurniWired settings)
    {
        var name = settings.StringParam;
        if (name.Length is < 1 or > MaxNameLength)
            return WiredValidation.Of("wiredfurni.error.variables.name_length");
        if (!NameSyntax().IsMatch(name))
            return WiredValidation.Of("wiredfurni.error.variables.name_syntax");
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex NameSyntax();
}

/// <summary>A variable furni can be given: a flag, or a value; for as long as the room is active, or kept.</summary>
[WiredBox("wf_var_furni")]
public sealed class FurniVariableBox : WiredVariableBox
{
    private const int HasValueParam = 0;
    private const int AvailabilityParam = 1;

    public override int Code => VariableCodes.FurniVariable;
    public override int Target => WiredVariableTargets.Furni;
    public override IReadOnlyList<int> DefaultIntParams => [0, WiredVariableAvailability.WhileRoomActive];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override WiredVariable Define(int boxId, FurniWired settings) => Created(boxId, settings,
        Param(settings, AvailabilityParam) == WiredVariableAvailability.Permanent
            ? WiredVariableAvailability.Permanent
            : WiredVariableAvailability.WhileRoomActive,
        Param(settings, HasValueParam) != 0, alwaysAvailable: false);
}

/// <summary>A variable users can be given: while they are in the room, kept, or kept and shared with other rooms.</summary>
[WiredBox("wf_var_user")]
public sealed class UserVariableBox : WiredVariableBox
{
    private const int AvailabilityParam = 0;
    private const int HasValueParam = 1;

    public override int Code => VariableCodes.UserVariable;
    public override int Target => WiredVariableTargets.User;
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableAvailability.WhileUserInRoom, 0];

    public override WiredVariable Define(int boxId, FurniWired settings) => Created(boxId, settings,
        Param(settings, AvailabilityParam) switch
        {
            WiredVariableAvailability.Permanent => WiredVariableAvailability.Permanent,
            WiredVariableAvailability.PermanentShared => WiredVariableAvailability.PermanentShared,
            _ => WiredVariableAvailability.WhileUserInRoom,
        },
        Param(settings, HasValueParam) != 0, alwaysAvailable: false);
}

/// <summary>A value the room itself holds: while it is active, kept, or kept and shared with other rooms.</summary>
[WiredBox("wf_var_room")]
public sealed class GlobalVariableBox : WiredVariableBox
{
    private const int AvailabilityParam = 0;

    public override int Code => VariableCodes.GlobalVariable;
    public override int Target => WiredVariableTargets.Global;
    public override IReadOnlyList<int> DefaultIntParams => [WiredVariableAvailability.WhileRoomActive];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override WiredVariable Define(int boxId, FurniWired settings) => Created(boxId, settings,
        Param(settings, AvailabilityParam) switch
        {
            WiredVariableAvailability.Permanent => WiredVariableAvailability.Permanent,
            WiredVariableAvailability.PermanentShared => WiredVariableAvailability.PermanentShared,
            _ => WiredVariableAvailability.WhileRoomActive,
        },
        hasValue: true, alwaysAvailable: true);
}

/// <summary>A variable that lives for one run of a stack: a flag, or a value, the run's boxes pass along.</summary>
[WiredBox("wf_var_context")]
public sealed class ContextVariableBox : WiredVariableBox
{
    private const int HasValueParam = 0;

    public override int Code => VariableCodes.ContextVariable;
    public override int Target => WiredVariableTargets.Context;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override WiredVariable Define(int boxId, FurniWired settings) =>
        Created(boxId, settings, WiredVariableAvailability.WhileRoomActive, Param(settings, HasValueParam) != 0, alwaysAvailable: false);
}
