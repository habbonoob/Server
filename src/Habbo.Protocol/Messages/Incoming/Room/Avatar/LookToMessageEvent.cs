using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>744 LookTo — the user turns to face a tile (the client sends it on clicking another
/// avatar, unless the room's wired handles that click).</summary>
[Incoming(ClientMessages.LookToMessageEvent)]
public sealed partial record LookToMessageEvent(int X, int Y);
