using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Layout;

/// <summary>2959 RoomEntryTileMessageComposer — where the door is and which way it faces. The
/// client keeps it aside and uses it while building the room from FloorHeightMap; without it
/// the client guesses the door by looking for a tile with void on three sides, which finds the
/// wrong tile — or none — in rooms whose doorway is not a notch in the outer wall.</summary>
[Message(ServerMessages.RoomEntryTileMessageComposer)]
public sealed partial record RoomEntryTileMessageComposer(int X, int Y, int Direction);
