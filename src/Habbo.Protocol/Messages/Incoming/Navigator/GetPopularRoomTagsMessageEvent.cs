using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>893 GetPopularRoomTags — the tag cloud.</summary>
[Incoming(ClientMessages.GetPopularRoomTagsMessageEvent)]
public sealed partial record GetPopularRoomTagsMessageEvent;
