using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Poll;

/// <summary>PollRejectEvent — turn a poll down</summary>
[Incoming(ClientMessages.PollRejectEvent)]
public sealed partial record PollRejectEvent(int PollId);
