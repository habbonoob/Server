using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Action;

/// <summary>1272 UseObjectMessageComposer — a user uses what they carry (drinks it).</summary>
[Message(ServerMessages.UseObjectMessageComposer)]
public sealed partial record UseObjectMessageComposer(int UserId, int ItemType);
