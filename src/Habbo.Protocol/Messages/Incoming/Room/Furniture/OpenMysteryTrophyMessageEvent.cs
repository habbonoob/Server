using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>OpenMysteryTrophyMessageEvent — open a mystery trophy box, engraving the trophy</summary>
[Incoming(ClientMessages.OpenMysteryTrophyMessageEvent)]
public sealed partial record OpenMysteryTrophyMessageEvent(int ItemId, string Inscription);
