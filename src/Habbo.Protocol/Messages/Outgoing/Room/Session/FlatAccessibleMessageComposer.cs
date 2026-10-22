using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>FlatAccessibleMessageComposer</summary>
[Message(ServerMessages.FlatAccessibleMessageComposer)]
public sealed partial record FlatAccessibleMessageComposer(int FlatId, string UserName);
