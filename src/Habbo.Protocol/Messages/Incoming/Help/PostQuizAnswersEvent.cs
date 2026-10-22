using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>PostQuizAnswersEvent — the answers given to a quiz, in the order its questions were asked</summary>
[Incoming(ClientMessages.PostQuizAnswersEvent)]
public sealed partial record PostQuizAnswersEvent(string QuizCode, IReadOnlyList<int> Answers);
