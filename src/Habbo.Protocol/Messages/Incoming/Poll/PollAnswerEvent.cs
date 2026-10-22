using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Poll;

/// <summary>PollAnswerEvent — answer a question of a poll</summary>
[Incoming(ClientMessages.PollAnswerEvent)]
public sealed partial record PollAnswerEvent(int PollId, int QuestionId, IReadOnlyList<string> Answers);
