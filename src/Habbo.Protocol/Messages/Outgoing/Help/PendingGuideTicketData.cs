using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>PendingGuideTicketData — a request the player already has open. What follows the type depends
/// on it: a tour or a bully report names the other side, a help request its description too, and a chat
/// review names the reported side and room to the reporter only.</summary>
[WireStruct]
public sealed partial record PendingGuideTicketData(
    int Type,
    int SecondsAgo,
    bool IsGuide,
    [property: WireWhen("NamesOtherParty")] string? OtherPartyName,
    [property: WireWhen("NamesOtherParty")] string? OtherPartyFigure,
    [property: WireWhen("HasDescription")] string? Description,
    [property: WireWhen("NamesRoom")] string? RoomName)
{
    public const int Tour = 0;
    public const int Help = 1;
    public const int Bully = 2;
    public const int ChatReview = 3;

    public bool NamesOtherParty => Type is Tour or Help or Bully || (Type == ChatReview && !IsGuide);

    public bool HasDescription => Type == Help;

    public bool NamesRoom => Type == ChatReview && !IsGuide;
}
