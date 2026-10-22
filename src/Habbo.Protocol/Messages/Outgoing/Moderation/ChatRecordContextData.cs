using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>One named fact about a chat record — "roomId", "roomName", "groupId", "threadId",
/// "messageId", "url" — typed as the client reads it: a boolean, an integer or text.</summary>
[WireStruct]
public sealed partial record ChatRecordContextData(
    string Key,
    byte Type,
    [property: WireWhen("IsBool")] bool? BoolValue,
    [property: WireWhen("IsInt")] int? IntValue,
    [property: WireWhen("IsString")] string? StringValue)
{
    private const byte BoolType = 0;
    private const byte IntType = 1;
    private const byte StringType = 2;

    public bool IsBool => Type == BoolType;
    public bool IsInt => Type == IntType;
    public bool IsString => Type == StringType;

    public static ChatRecordContextData Of(string key, int value) => new(key, IntType, null, value, null);

    public static ChatRecordContextData Of(string key, string value) => new(key, StringType, null, null, value);

    public static ChatRecordContextData Of(string key, bool value) => new(key, BoolType, value, null, null);
}
