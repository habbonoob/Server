using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>SetRoomSessionTagsMessageEvent</summary>
[Incoming(ClientMessages.SetRoomSessionTagsMessageEvent)]
public sealed partial record SetRoomSessionTagsMessageEvent(string Tag1, string Tag2);
