using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionInvitedToGuideRoomMessageComposer — the guide's room the requester is invited to, 0 when the guide is in none</summary>
[Message(ServerMessages.GuideSessionInvitedToGuideRoomMessageComposer)]
public sealed partial record GuideSessionInvitedToGuideRoomMessageComposer(int RoomId, string RoomName);
