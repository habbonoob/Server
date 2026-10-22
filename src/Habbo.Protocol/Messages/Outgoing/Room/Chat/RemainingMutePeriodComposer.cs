using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>RemainingMutePeriodComposer</summary>
[Message(ServerMessages.RemainingMutePeriodComposer)]
public sealed partial record RemainingMutePeriodComposer(int SecondsRemaining);
