using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>2216 FlatCreatedComposer — the client opens the new room's settings on this.</summary>
[Message(ServerMessages.FlatCreatedComposer)]
public sealed partial record FlatCreatedComposer(int RoomId, string RoomName);
