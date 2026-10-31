namespace Habbo.Silo.Abstractions.Wired;

/// <summary>What a send-link effect sends through a WIRED Link: a message (send link message), or a dataframe of named
/// values (send link dataframe); the room it was sent from, which the receiving stacks read as
/// @event.link.source_room_id; and how many links it has passed through on the way, counting each receive that sent
/// on.</summary>
[GenerateSerializer]
public sealed partial record WiredLinkPayload(
    [property: Id(0)] int SourceRoomId,
    [property: Id(1)] bool IsDataframe,
    [property: Id(2)] string Message,
    [property: Id(3)] List<WiredLinkRow> Rows,
    [property: Id(4)] int Hops);

/// <summary>One row of a link dataframe: its key, and its value, a number or a text (a variable's text, or a custom
/// message) as the sending row's source made it.</summary>
[GenerateSerializer]
public sealed partial record WiredLinkRow(
    [property: Id(0)] string Key,
    [property: Id(1)] int? Number,
    [property: Id(2)] string? Text);

/// <summary>A payload on its way to the Link (by item id) that receives it in another room, over a network.</summary>
[GenerateSerializer]
public sealed partial record WiredLinkDelivery(
    [property: Id(0)] int ItemId,
    [property: Id(1)] int NetworkId,
    [property: Id(2)] WiredLinkPayload Payload);
