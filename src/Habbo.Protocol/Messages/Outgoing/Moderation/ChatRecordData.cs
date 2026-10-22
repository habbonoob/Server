using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>A piece of chat the mod tool shows: what kind it is, a few named facts about where it
/// happened (the room, the forum thread), and the lines themselves.</summary>
[WireStruct]
public sealed partial record ChatRecordData(
    byte RecordType,
    [property: WireCount(WireType.I16)] IReadOnlyList<ChatRecordContextData> Context,
    [property: WireCount(WireType.I16)] IReadOnlyList<ChatlineData> Chatlog)
{
    // what the chat log window heads the record with
    public const byte Room = 1;
    public const byte InstantMessages = 2;
    public const byte ForumThread = 3;
    public const byte ForumMessage = 4;
    public const byte Selfie = 5;
    public const byte Photo = 6;
}
