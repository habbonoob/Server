using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ChatlineData</summary>
[WireStruct]
public sealed partial record ChatlineData(string TimeStamp, int ChatterId, string ChatterName, string Message, bool HasHighlighting);
