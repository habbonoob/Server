using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>QuizDataMessageComposer — the questions of a quiz, in the order to ask them</summary>
[Message(ServerMessages.QuizDataMessageComposer)]
public sealed partial record QuizDataMessageComposer(string QuizCode, IReadOnlyList<int> QuestionIds);
