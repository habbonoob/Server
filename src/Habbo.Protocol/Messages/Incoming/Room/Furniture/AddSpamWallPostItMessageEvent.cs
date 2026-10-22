using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>AddSpamWallPostItMessageEvent — a note written before it goes up, by someone without rights</summary>
[Incoming(ClientMessages.AddSpamWallPostItMessageEvent)]
public sealed partial record AddSpamWallPostItMessageEvent(int ItemId, string Location, string ColorHex, string Text);
