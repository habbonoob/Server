using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Discord;

/// <summary>DiscordPreferencesComposer — how the player shows on Discord</summary>
[Message(ServerMessages.DiscordPreferencesComposer)]
public sealed partial record DiscordPreferencesComposer(int Version, bool ShowHabbo, bool ShareActivity, bool HideInHiddenRooms, bool AllowJoining);
