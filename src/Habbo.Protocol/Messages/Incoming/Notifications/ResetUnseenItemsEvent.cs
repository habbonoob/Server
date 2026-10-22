using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Notifications;

/// <summary>2303 ResetUnseenItems — the player opened a category, so its badge is cleared.</summary>
[Incoming(ClientMessages.ResetUnseenItemsEvent)]
public sealed partial record ResetUnseenItemsEvent(int Category);
