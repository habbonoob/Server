using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>UpdateRoomThumbnailMessageEvent</summary>
[Incoming(ClientMessages.UpdateRoomThumbnailMessageEvent)]
public sealed partial record UpdateRoomThumbnailMessageEvent(int FlatId, int BackgroundImageId, int FrontImageId, int ForegroundImageId);
