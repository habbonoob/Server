using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>YouArePlayingGameMessageComposer</summary>
[Message(ServerMessages.YouArePlayingGameMessageComposer)]
public sealed partial record YouArePlayingGameMessageComposer(bool IsPlaying);
