using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>RoomVisitsComposer</summary>
[Message(ServerMessages.RoomVisitsComposer)]
public sealed partial record RoomVisitsComposer(int UserId, string UserName, IReadOnlyList<RoomVisitData> Rooms);
