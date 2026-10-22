using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollErrorComposer — the poll is not there to take</summary>
[Message(ServerMessages.PollErrorComposer)]
public sealed partial record PollErrorComposer;
