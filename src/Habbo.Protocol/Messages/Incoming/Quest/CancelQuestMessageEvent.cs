using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Quest;

/// <summary>CancelQuestMessageEvent — give up whichever quest is taken on.</summary>
[Incoming(ClientMessages.CancelQuestMessageEvent)]
public sealed partial record CancelQuestMessageEvent;
