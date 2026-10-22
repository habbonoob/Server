namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>A Builders Club offer to be borrowed and put down in a room: the offer as the client named it (echoed in
/// a placement warning) and the furniture it lends. The membership that lends it is the room owner's, which the
/// room looks up itself.</summary>
[GenerateSerializer]
public sealed partial record BuildersClubPlacement(
    [property: Id(0)] int PageId,
    [property: Id(1)] int OfferId,
    [property: Id(2)] string ExtraParam,
    [property: Id(3)] int DefinitionId,
    [property: Id(4)] string ProductParam,
    /// <summary>The player agreed to the placement warning.</summary>
    [property: Id(5)] bool Confirmed);
