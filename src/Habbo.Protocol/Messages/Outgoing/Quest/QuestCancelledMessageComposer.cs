using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Quest;

/// <summary>QuestCancelledMessageComposer — the player is off a quest: given up, or (expired) its campaign ended,
/// which the client tells them of.</summary>
[Message(ServerMessages.QuestCancelledMessageComposer)]
public sealed partial record QuestCancelledMessageComposer(bool Expired, QuestMessageData Quest);
