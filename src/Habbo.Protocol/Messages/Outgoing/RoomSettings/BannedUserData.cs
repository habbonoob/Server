using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>BannedUserData</summary>
[WireStruct]
public sealed partial record BannedUserData(int UserId, string UserName);
