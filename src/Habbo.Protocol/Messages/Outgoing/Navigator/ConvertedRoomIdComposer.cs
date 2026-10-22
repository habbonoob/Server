using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>ConvertedRoomIdComposer</summary>
[Message(ServerMessages.ConvertedRoomIdComposer)]
public sealed partial record ConvertedRoomIdComposer(string GlobalId, int ConvertedId);
