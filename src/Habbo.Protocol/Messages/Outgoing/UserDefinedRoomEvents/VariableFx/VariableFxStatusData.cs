using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>A variable display's value for one holder: its key ("display id|variable id"), whether this is the value
/// it starts with rather than a change, the holder (a user's room index, or a furni id), the value, a range of its own
/// if it has one, and extras.</summary>
[WireStruct]
public sealed partial record VariableFxStatusData(
    string StatusKey, bool Initialize, bool IsUserEntity, int EntityId, long Value,
    [property: WirePresence] VariableFxRangeData? Override,
    IReadOnlyList<VariableFxExtraData> Extras);
