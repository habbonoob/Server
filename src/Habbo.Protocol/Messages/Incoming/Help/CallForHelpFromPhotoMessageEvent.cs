using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromPhotoMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpFromPhotoMessageEvent)]
public sealed partial record CallForHelpFromPhotoMessageEvent(string PhotoId, int RoomId, int ReportedUserId, int TopicId, int RoomObjectId, string ReporterName, string ReporterEmail);
