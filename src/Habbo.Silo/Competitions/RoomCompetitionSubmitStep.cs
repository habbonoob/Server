namespace Habbo.Silo.Competitions;

/// <summary>How far an owner has got entering their room (SubmitRoomToCompetitionMessageComposer).</summary>
public enum RoomCompetitionSubmitStep
{
    Ask = 0,
    AcceptRules = 1,
    Submit = 2,
    Confirm = 3,
}
