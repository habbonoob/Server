using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>A user turned by wired.</summary>
[WireStruct]
public sealed partial record WiredUserDirectionData(int RoomIndex, int BodyDirection, int HeadDirection);
