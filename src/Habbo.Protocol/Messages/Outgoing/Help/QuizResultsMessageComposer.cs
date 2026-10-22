using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>QuizResultsMessageComposer — which questions were answered wrong</summary>
[Message(ServerMessages.QuizResultsMessageComposer)]
public sealed partial record QuizResultsMessageComposer(string QuizCode, IReadOnlyList<int> QuestionIdsForWrongAnswers);
