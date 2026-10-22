using Habbo.Domain.Polls;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollQuestionData — a question of a poll. Its choices' count is the answer count the client reads,
/// and it reads that many choices for a choice question; a free-text question has none.</summary>
[WireStruct]
public sealed partial record PollQuestionData(
    int QuestionId,
    int SortOrder,
    int QuestionType,
    string QuestionText,
    int QuestionCategory,
    int QuestionAnswerType,
    IReadOnlyList<PollChoiceData> Choices)
{
    public static PollQuestionData From(PollQuestion question) => new(
        question.Id, question.SortOrder, question.Type, question.Text, question.Category, question.AnswerType,
        question.Type is PollQuestion.SingleChoice or PollQuestion.MultipleChoice
            ? question.Choices.OrderBy(c => c.Position).Select(c => new PollChoiceData(c.Value, c.Text, c.ChoiceType)).ToList()
            : []);
}
