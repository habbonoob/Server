using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>PatternMatchData</summary>
[WireStruct]
public sealed partial record PatternMatchData(string Pattern, int StartIndex, int EndIndex);
