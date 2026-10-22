using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GetQuizQuestionsEvent — the questions of a quiz</summary>
[Incoming(ClientMessages.GetQuizQuestionsEvent)]
public sealed partial record GetQuizQuestionsEvent(string QuizCode);
