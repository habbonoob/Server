using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Avatar;

/// <summary>1170 WardrobeMessageComposer.</summary>
[Message(ServerMessages.WardrobeMessageComposer)]
public sealed partial record WardrobeMessageComposer(int State, IReadOnlyList<OutfitData> Outfits);
