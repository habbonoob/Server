using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Avatar;

/// <summary>3771 SaveWardrobeOutfit — keep a look in one of the wardrobe's slots.</summary>
[Incoming(ClientMessages.SaveWardrobeOutfitMessageEvent)]
public sealed partial record SaveWardrobeOutfitMessageEvent(int Slot, string Figure, string Gender);
