using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3167 BlockListMessageComposer — who the player has blocked. Muting is a separate list;
/// the client keeps the two in different managers.</summary>
[Message(ServerMessages.BlockListMessageComposer)]
public sealed partial record BlockListMessageComposer(IReadOnlyList<int> UserIds);
