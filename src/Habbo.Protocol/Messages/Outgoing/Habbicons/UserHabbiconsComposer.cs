using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>UserHabbicons (September 2026) — the Habbicons the player owns, and those they used last, most recent
/// first (the chat input's picker lists ten). Habbo sends it at login, after the reward tracks.</summary>
[Message(ServerMessages.UserHabbiconsComposer)]
public sealed partial record UserHabbiconsComposer(IReadOnlyList<UserHabbiconData> Habbicons, IReadOnlyList<int> RecentHabbiconIds);
