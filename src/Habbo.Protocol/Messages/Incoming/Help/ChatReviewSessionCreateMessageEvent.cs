using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>ChatReviewSessionCreateMessageEvent — report someone for bullying, for the guardians to judge</summary>
[Incoming(ClientMessages.ChatReviewSessionCreateMessageEvent)]
public sealed partial record ChatReviewSessionCreateMessageEvent(int ReportedUserId, int RoomId);
