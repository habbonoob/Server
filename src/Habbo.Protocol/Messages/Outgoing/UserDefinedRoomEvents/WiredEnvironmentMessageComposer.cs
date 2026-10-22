using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>2312 WiredEnvironmentMessageComposer — whether the room's wired listens for clicks on
/// users, and the achievements its wired may award. While it listens, the client neither turns the
/// clicker nor opens the avatar menu itself: it asks the server, which answers with
/// <see cref="WiredClickUserResponseComposer"/>. Habbo sends it on every room entry, wired or not.</summary>
[Message(ServerMessages.WiredEnvironmentMessageComposer)]
public sealed partial record WiredEnvironmentMessageComposer(bool HasClickUserWired, IReadOnlyList<string> EnabledAchievements);
