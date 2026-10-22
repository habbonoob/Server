using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>YouAreSpectatorMessageComposer</summary>
[Message(ServerMessages.YouAreSpectatorMessageComposer)]
public sealed partial record YouAreSpectatorMessageComposer(int FlatId);
