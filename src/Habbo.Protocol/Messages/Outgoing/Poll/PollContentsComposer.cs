using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollContentsComposer — a poll's questions</summary>
[Message(ServerMessages.PollContentsComposer)]
public sealed partial record PollContentsComposer(int Id, string StartMessage, string EndMessage, IReadOnlyList<PollQuestionWithFollowUpsData> Questions, bool NpsPoll);
