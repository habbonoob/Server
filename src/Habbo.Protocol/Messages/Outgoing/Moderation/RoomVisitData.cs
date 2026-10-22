using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>RoomVisitData</summary>
[WireStruct]
public sealed partial record RoomVisitData(int RoomId, string RoomName, int EnterHour, int EnterMinute);
