namespace Habbo.Domain.Rooms;

/// <summary>An effect an avatar can wear — a hoverboard, a team colour, the freeze of a wired game —
/// as Habbo's effect map (seed/effectmap.xml) lists it. The library is the effect's name there,
/// which is how the server finds the effects it puts on avatars itself.</summary>
public sealed class AvatarEffect
{
    /// <summary>Habbo's number for the effect: what goes on the wire.</summary>
    public int Id { get; set; }

    /// <summary>The effect's library, e.g. "WiredTeamRed".</summary>
    public required string Library { get; set; }

    /// <summary>"fx", or "dance" for the dances (which the map also lists, under other ids).</summary>
    public required string Type { get; set; }

    public int Revision { get; set; }
}
