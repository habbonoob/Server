using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollChoiceData — a choice a question offers</summary>
[WireStruct]
public sealed partial record PollChoiceData(string Value, string Text, int ChoiceType);
