using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>FloodControlMessageComposer</summary>
[Message(ServerMessages.FloodControlMessageComposer)]
public sealed partial record FloodControlMessageComposer(int Seconds);
