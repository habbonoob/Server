namespace Habbo.Silo.Abstractions.Wired;

/// <summary>The families of wired box. Each opens its own dialog and is saved by its own message;
/// furni_interactions.json gives every wf_ item the interaction name of its family.</summary>
public enum WiredKind
{
    /// <summary>Starts a stack when something happens: a user walks on a furni, says a word.</summary>
    Trigger,

    /// <summary>What a stack does.</summary>
    Effect,

    /// <summary>Must hold for a stack's effects to run.</summary>
    Condition,

    /// <summary>Chooses the furni or users a stack works on.</summary>
    Selector,

    /// <summary>Changes how a stack runs: one random effect, say.</summary>
    Addon,

    /// <summary>Declares a variable the room's wired can read and write.</summary>
    Variable,
}

/// <summary>A wired box's dialog saved, as any of the six update messages carries it: the fields
/// every box has, and those only one family uses (left at their defaults by the others).</summary>
[GenerateSerializer]
public sealed partial record WiredSaveData(
    [property: Id(0)] WiredKind Kind,
    [property: Id(1)] int ItemId,
    [property: Id(2)] List<int> IntParams,
    [property: Id(3)] string StringParam,
    [property: Id(4)] List<int> StuffIds,
    [property: Id(5)] List<int> StuffIds2,
    [property: Id(6)] List<int> FurniSources,
    [property: Id(7)] List<int> UserSources,
    [property: Id(8)] List<string> VariableIds,
    [property: Id(9)] int DelayInPulses = 0,
    [property: Id(10)] int Quantifier = 0,
    [property: Id(11)] bool IsFilter = false,
    [property: Id(12)] bool IsInvert = false);
