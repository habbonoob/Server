using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>SpecialSystemChat (September 2026) — a line the client draws itself over an avatar, by type: the 67
/// expression's "666… 777…", which Habbo sends along with the expression.</summary>
[Message(ServerMessages.SpecialSystemChatMessageComposer)]
public sealed partial record SpecialSystemChatMessageComposer(int RoomIndex, int SpecialSystemType);
