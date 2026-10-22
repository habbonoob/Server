using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Discord;

/// <summary>SetDiscordPreferencesMessageEvent — change how the player shows on Discord</summary>
[Incoming(ClientMessages.SetDiscordPreferencesMessageEvent)]
public sealed partial record SetDiscordPreferencesMessageEvent(int Version, bool ShowHabbo, bool ShareActivity, bool HideInHiddenRooms, bool AllowJoining);
