using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollQuestionWithFollowUpsData — a question and those that follow it up</summary>
[WireStruct]
public sealed partial record PollQuestionWithFollowUpsData(PollQuestionData Question, IReadOnlyList<PollQuestionData> Children);
