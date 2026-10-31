using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>SendHabbiconMessageEvent (September 2026) — a Habbicon sent in the console, in place of text.</summary>
[Incoming(ClientMessages.SendHabbiconMessageEvent)]
public sealed partial record SendHabbiconMessageEvent(
    /// <summary>The friend's account id, or a group chat's id negated.</summary>
    int ChatId,
    int HabbiconId,
    /// <summary>The sender's own counter, echoed back so their client can match it up.</summary>
    int ConfirmationId);
