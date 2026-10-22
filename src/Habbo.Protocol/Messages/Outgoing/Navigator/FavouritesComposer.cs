using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>3738 FavouritesComposer — the starred rooms, plus how many a user may keep.</summary>
[Message(ServerMessages.FavouritesComposer)]
public sealed partial record FavouritesComposer(int Limit, IReadOnlyList<int> RoomIds);
