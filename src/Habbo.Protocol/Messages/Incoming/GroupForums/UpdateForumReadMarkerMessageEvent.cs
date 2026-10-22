using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>UpdateForumReadMarkerMessageEvent</summary>
[Incoming(ClientMessages.UpdateForumReadMarkerMessageEvent)]
public sealed partial record UpdateForumReadMarkerMessageEvent(IReadOnlyList<ForumReadMarkerData> Markers);
