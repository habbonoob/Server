using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Competition;

/// <summary>NoOwnedRoomsAlertMessageComposer — the player has no room that could be entered</summary>
[Message(ServerMessages.NoOwnedRoomsAlertMessageComposer)]
public sealed partial record NoOwnedRoomsAlertMessageComposer;
