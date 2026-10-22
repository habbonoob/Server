using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>215 BuildersClubFurniCountMessageComposer — the Builders Club items the player has out ("Borrowed items:
/// %COUNT%/%LIMIT%"). Until it arrives the warehouse counts -1 and lets nothing be placed.</summary>
[Message(ServerMessages.BuildersClubFurniCountMessageComposer)]
public sealed partial record BuildersClubFurniCountMessageComposer(int FurniCount);
