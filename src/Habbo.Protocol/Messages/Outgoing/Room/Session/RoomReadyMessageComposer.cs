using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>675 RoomReadyMessageComposer — the model name and room id.</summary>
[Message(ServerMessages.RoomReadyMessageComposer)]
public sealed partial record RoomReadyMessageComposer(string ModelName, int RoomId);
