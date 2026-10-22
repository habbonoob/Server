using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Poll;

/// <summary>PollStartEvent — take a poll that was offered</summary>
[Incoming(ClientMessages.PollStartEvent)]
public sealed partial record PollStartEvent(int PollId);
