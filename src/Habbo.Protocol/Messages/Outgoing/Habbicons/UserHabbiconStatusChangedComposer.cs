using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>UserHabbiconStatusChanged (September 2026) — one Habbicon's state for the player changed: bought,
/// claimed, given (the client tells the player of a new one), starred or unstarred.</summary>
[Message(ServerMessages.UserHabbiconStatusChangedComposer)]
public sealed partial record UserHabbiconStatusChangedComposer(int HabbiconId, HabbiconState State);
