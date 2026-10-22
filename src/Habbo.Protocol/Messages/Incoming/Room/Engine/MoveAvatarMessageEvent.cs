using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>2256 MoveAvatar — the tile the player clicked.</summary>
[Incoming(ClientMessages.MoveAvatarMessageEvent)]
public sealed partial record MoveAvatarMessageEvent(int X, int Y);
