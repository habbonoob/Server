using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>1817 WiredGetRoomSettings — the wired menu's settings tab opens.</summary>
[Incoming(ClientMessages.WiredGetRoomSettingsMessageEvent)]
public sealed partial record WiredGetRoomSettingsMessageEvent;
