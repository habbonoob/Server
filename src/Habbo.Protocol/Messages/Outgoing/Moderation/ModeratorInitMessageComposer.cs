using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorInitMessageComposer</summary>
[Message(ServerMessages.ModeratorInitMessageComposer)]
public sealed partial record ModeratorInitMessageComposer(IReadOnlyList<IssueMessageData> Issues, IReadOnlyList<string> MessageTemplates, IReadOnlyList<string> UnusedTemplates, bool CfhPermission, bool ChatlogsPermission, bool AlertPermission, bool KickPermission, bool BanPermission, bool RoomAlertPermission, bool RoomKickPermission, IReadOnlyList<string> RoomMessageTemplates);
