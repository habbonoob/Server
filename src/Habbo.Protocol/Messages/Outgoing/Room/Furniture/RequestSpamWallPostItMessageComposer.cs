using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>RequestSpamWallPostItMessageComposer — asks for a note's text before it goes up</summary>
[Message(ServerMessages.RequestSpamWallPostItMessageComposer)]
public sealed partial record RequestSpamWallPostItMessageComposer(int ItemId, string Location);
