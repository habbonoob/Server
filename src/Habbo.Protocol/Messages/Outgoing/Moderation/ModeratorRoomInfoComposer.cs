using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorRoomInfoComposer</summary>
[Message(ServerMessages.ModeratorRoomInfoComposer)]
public sealed partial record ModeratorRoomInfoComposer(int FlatId, int UserCount, bool OwnerInRoom, int OwnerId, string OwnerName, bool Exists, string Name, string Description, IReadOnlyList<string> Tags);
