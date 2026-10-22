using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>ForumReadMarkerData — one forum's read marker</summary>
[WireStruct]
public sealed partial record ForumReadMarkerData(int GroupId, int LastReadMessageIndex, bool Reset);
