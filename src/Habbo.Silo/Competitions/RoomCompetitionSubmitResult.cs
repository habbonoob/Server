namespace Habbo.Silo.Competitions;

/// <summary>Where entering a room stands, as roomcompetition.*.submit.&lt;value&gt; explains it.</summary>
public enum RoomCompetitionSubmitResult
{
    /// <summary>The room is in the competition.</summary>
    Entered = 0,

    CanSubmit = 1,

    /// <summary>Submitted, to be confirmed: once in, a room cannot be taken back.</summary>
    ConfirmSubmit = 2,

    MissingFurni = 3,

    /// <summary>Voters could not get in.</summary>
    DoorNotOpen = 4,

    /// <summary>The room was made before the competition began.</summary>
    RoomTooOld = 5,

    AcceptRules = 6,
}
