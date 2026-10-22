using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>YouAreNotSpectatorMessageComposer</summary>
[Message(ServerMessages.YouAreNotSpectatorMessageComposer)]
public sealed partial record YouAreNotSpectatorMessageComposer(int FlatId);
